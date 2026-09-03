using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;

namespace GestionQ.CajaPOS
{
    public class SyncWorker
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private CancellationTokenSource _cts = new();
        
        public event Action OnSyncCompleted;
        public event Action<string> OnSyncError;

        public SyncWorker(string serverUrl)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(async () => await RunLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PerformSyncAsync();
                }
                catch (Exception ex)
                {
                    OnSyncError?.Invoke(ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }

        public string PosIdentifier = Environment.MachineName;

        public async Task PerformSyncAsync()
        {
            bool dataChanged = false;
            using var db = new LocalDbContext();
            
            // 1. PUSH
            var unsyncedSales = await db.Sales.Include(s => s.Items).Include(s => s.Payments).Where(s => !s.IsSynced).ToListAsync();
            var unsyncedMovements = await db.Movements.Where(m => !m.IsSynced).ToListAsync();
            var unsyncedCustomers = await db.Customers.Where(c => EF.Property<bool>(c, "IsSynced") == false).ToListAsync();
            var unsyncedRegisters = await db.OfflineCashRegisters.Where(r => !r.IsSynced).ToListAsync();
            var allOfflineRegisters = await db.OfflineCashRegisters.ToListAsync();

            if (unsyncedSales.Any() || unsyncedMovements.Any() || unsyncedCustomers.Any() || unsyncedRegisters.Any())
            {
                var pushRequest = new SyncPushRequest
                {
                    PosIdentifier = this.PosIdentifier,
                    OfflineCashRegisters = unsyncedRegisters.Select(r => new OfflineCashRegisterSyncDto { GlobalId = r.GlobalId, UserId = r.UserId, OpeningDate = r.OpeningDate, ClosingDate = r.ClosingDate, InitialBalance = r.InitialBalance, FinalCashBalance = r.FinalCashBalance }).ToList(),
                    NewCustomers = unsyncedCustomers.Select(c => new CustomerSyncDto { Dni = c.Dni, Name = c.Name, Email = c.Email, Phone = c.Phone, Cuit = c.Cuit }).ToList(),
                    Sales = unsyncedSales.Select(s => {
                        var customer = s.CustomerId.HasValue ? db.Customers.Find(s.CustomerId.Value) : null;
                        var offlineReg = allOfflineRegisters.FirstOrDefault(r => r.Id == s.CashRegisterId);
                        return new SaleSyncDto { GlobalId = s.GlobalId, Date = s.Date, TotalAmount = s.TotalAmount, SubTotal = s.SubTotal, DiscountAmount = s.DiscountAmount, PaymentDiscountAmount = s.PaymentDiscountAmount, UserId = s.UserId, CashRegisterId = offlineReg == null ? s.CashRegisterId : offlineReg.ServerCashRegisterId, OfflineCashRegisterGlobalId = offlineReg?.GlobalId, CustomerDni = customer?.Dni, RequestElectronicInvoice = s.RequestElectronicInvoice, IsCancelled = s.IsCancelled, CancellationDate = s.CancellationDate, Items = s.Items.Select(i => new SaleItemSyncDto { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountAmount = i.DiscountAmount }).ToList(), Payments = s.Payments.Select(p => new SalePaymentSyncDto { PaymentMethodId = p.PaymentMethodId, Amount = p.Amount, TransactionReference = p.TransactionReference }).ToList() };
                    }).ToList(),
                    Movements = unsyncedMovements.Select(m => {
                        var offlineReg = allOfflineRegisters.FirstOrDefault(r => r.Id == m.CashRegisterId);
                        return new MovementSyncDto { GlobalId = m.GlobalId, Amount = m.Amount, Type = m.Type, Description = m.Description, Date = m.Date, CashRegisterId = offlineReg == null ? m.CashRegisterId : offlineReg.ServerCashRegisterId, OfflineCashRegisterGlobalId = offlineReg?.GlobalId };
                    }).ToList()
                };

                var pushResponse = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/sync/push", pushRequest);

                if (pushResponse.IsSuccessStatusCode)
                {
                    try {
                        var responseData = await pushResponse.Content.ReadFromJsonAsync<SyncPushResponse>();
                        if (responseData?.RegisterIdMap != null) {
                            foreach(var r in unsyncedRegisters) {
                                if (responseData.RegisterIdMap.TryGetValue(r.GlobalId, out int serverId)) {
                                    r.ServerCashRegisterId = serverId;
                                }
                            }
                        }
                    } catch { }
                    foreach (var s in unsyncedSales) s.IsSynced = true;
                    foreach (var m in unsyncedMovements) m.IsSynced = true;
                    foreach (var r in unsyncedRegisters) r.IsSynced = true;
                    foreach (var c in unsyncedCustomers) db.Entry(c).Property("IsSynced").CurrentValue = true;
                    await db.SaveChangesAsync();
                }
            }

            // 2. PULL
            var lastSyncDate = await db.Products.MaxAsync(p => (DateTime?)p.LastModified);
            if (await db.Products.AnyAsync(p => p.ImageUrl == null))
            {
                lastSyncDate = null;
            }
            var pullRequest = new SyncPullRequest { 
                LastSyncDate = lastSyncDate,
                PosIdentifier = this.PosIdentifier
            };

            var pullResponse = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/sync/pull", pullRequest);

            if (pullResponse.IsSuccessStatusCode)
            {
                var result = await pullResponse.Content.ReadFromJsonAsync<SyncPullResponse>();
                if (result != null)
                {
                    if (result.CompanyInfo != null)
                    {
                        var settings = await db.SystemSettings.ToListAsync();
                        db.SystemSettings.RemoveRange(settings);
                        
                        db.SystemSettings.Add(new SystemSetting { Key = "CompanyName", Value = result.CompanyInfo.Name });
                        db.SystemSettings.Add(new SystemSetting { Key = "CompanyLogoUrl", Value = result.CompanyInfo.LogoUrl });
                        
                        if (!string.IsNullOrEmpty(result.CompanyInfo.LogoUrl))
                        {
                            await DownloadImageAsync(result.CompanyInfo.LogoUrl);
                        }
                    }

                    if (result.Users != null && result.Users.Any())
                    {
                        var usersSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "PosUsers");
                        string usersText = System.Text.Json.JsonSerializer.Serialize(result.Users);
                        if (usersSetting == null)
                        {
                            db.SystemSettings.Add(new SystemSetting { Key = "PosUsers", Value = usersText });
                        }
                        else
                        {
                            usersSetting.Value = usersText;
                        }
                    }

                    if (result.Products.Any())
                    {
                        foreach (var pDto in result.Products)
                        {
                            var localProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == pDto.Id);
                            if (localProduct == null)
                            {
                                db.Products.Add(new Product { Id = pDto.Id, InternalCode = pDto.InternalCode, Barcode = pDto.Barcode, Name = pDto.Name, Price = pDto.Price, Stock = pDto.Stock, IsActive = pDto.IsActive, LastModified = pDto.LastModified, CreationDate = DateTime.Now, ImageUrl = pDto.ImageUrl });
                            }
                            else
                            {
                                localProduct.InternalCode = pDto.InternalCode; localProduct.Barcode = pDto.Barcode; localProduct.Name = pDto.Name; localProduct.Price = pDto.Price; localProduct.Stock = pDto.Stock; localProduct.IsActive = pDto.IsActive; localProduct.LastModified = pDto.LastModified; localProduct.ImageUrl = pDto.ImageUrl;
                            }

                            if (!string.IsNullOrEmpty(pDto.ImageUrl))
                            {
                                await DownloadImageAsync(pDto.ImageUrl);
                            }
                        }
                    }
                    
                    if (result.Customers.Any())
                    {
                        // Resync simple: clear and add (offline mode cache)
                        var currentCustomers = await db.Customers.ToListAsync();
                        db.Customers.RemoveRange(currentCustomers);
                        foreach (var c in result.Customers)
                        {
                            db.Customers.Add(new Customer { Id = c.Id, Name = c.Name, Email = c.Email, Phone = c.Phone, Dni = c.Dni, Cuit = c.Cuit, IsActive = c.IsActive });
                        }
                    }

                    if (result.Departments.Any())
                    {
                        var currentDepts = await db.Departments.ToListAsync();
                        db.Departments.RemoveRange(currentDepts);
                        foreach (var d in result.Departments)
                        {
                            db.Departments.Add(new Department { Id = d.Id, Name = d.Name, Hotkey = d.Hotkey, VirtualProductId = d.VirtualProductId, VatRateId = 1 });
                        }
                    }

                    if (result.PaymentMethods.Any())
                    {
                        var currentPms = await db.PaymentMethods.ToListAsync();
                        db.PaymentMethods.RemoveRange(currentPms);
                        foreach (var pm in result.PaymentMethods)
                        {
                            db.PaymentMethods.Add(new PaymentMethod { Id = pm.Id, Name = pm.Name, IsActive = pm.IsActive, DiscountPercentage = pm.DiscountPercentage, DiscountValidFrom = pm.DiscountValidFrom, DiscountValidTo = pm.DiscountValidTo });
                        }
                    }

                    if (result.ActivePromotions != null)
                    {
                        var promosSetting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "ActivePromotions");
                        string promosText = result.ActivePromotions.Any() ? System.Text.Json.JsonSerializer.Serialize(result.ActivePromotions) : "[]";
                        
                        if (promosSetting == null)
                        {
                            db.SystemSettings.Add(new SystemSetting { Key = "ActivePromotions", Value = promosText });
                        }
                        else
                        {
                            promosSetting.Value = promosText;
                        }
                    }

                    await db.SaveChangesAsync();
                    dataChanged = true;
                }
            }
            
            // Para simplificar, asumimos que siempre notificamos si hubo conexión exitosa
            OnSyncCompleted?.Invoke();
        }

        private async Task DownloadImageAsync(string relativeUrl)
        {
            try
            {
                string localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeUrl.TrimStart('/'));
                string directory = System.IO.Path.GetDirectoryName(localPath);
                if (directory != null && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                bool isLogo = relativeUrl.Contains("logo.png");
                if (isLogo || !System.IO.File.Exists(localPath))
                {
                    var fullUrl = $"{_serverUrl}/{relativeUrl.TrimStart('/')}";
                    var imageBytes = await _httpClient.GetByteArrayAsync(fullUrl);
                    await System.IO.File.WriteAllBytesAsync(localPath, imageBytes);
                }
            }
            catch
            {
                // Ignore download errors to not break sync
            }
        }
    }
}

