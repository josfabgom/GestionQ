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

            if (unsyncedSales.Any() || unsyncedMovements.Any() || unsyncedCustomers.Any())
            {
                var pushRequest = new SyncPushRequest
                {
                    PosIdentifier = this.PosIdentifier,
                    NewCustomers = unsyncedCustomers.Select(c => new CustomerSyncDto { Dni = c.Dni, Name = c.Name, Email = c.Email, Phone = c.Phone, Cuit = c.Cuit }).ToList(),
                    Sales = unsyncedSales.Select(s => {
                        var customer = s.CustomerId.HasValue ? db.Customers.Find(s.CustomerId.Value) : null;
                        return new SaleSyncDto { GlobalId = s.GlobalId, Date = s.Date, TotalAmount = s.TotalAmount, SubTotal = s.SubTotal, DiscountAmount = s.DiscountAmount, UserId = s.UserId, CashRegisterId = s.CashRegisterId, CustomerDni = customer?.Dni, RequestElectronicInvoice = s.RequestElectronicInvoice, Items = s.Items.Select(i => new SaleItemSyncDto { ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, DiscountAmount = i.DiscountAmount }).ToList(), Payments = s.Payments.Select(p => new SalePaymentSyncDto { PaymentMethodId = p.PaymentMethodId, Amount = p.Amount, TransactionReference = p.TransactionReference }).ToList() };
                    }).ToList(),
                    Movements = unsyncedMovements.Select(m => new MovementSyncDto { GlobalId = m.GlobalId, Amount = m.Amount, Type = m.Type, Description = m.Description, Date = m.Date, CashRegisterId = m.CashRegisterId }).ToList()
                };

                var pushResponse = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/sync/push", pushRequest);

                if (pushResponse.IsSuccessStatusCode)
                {
                    foreach (var s in unsyncedSales) s.IsSynced = true;
                    foreach (var m in unsyncedMovements) m.IsSynced = true;
                    foreach (var c in unsyncedCustomers) db.Entry(c).Property("IsSynced").CurrentValue = true;
                    await db.SaveChangesAsync();
                }
            }

            // 2. PULL
            var lastSyncDate = await db.Products.MaxAsync(p => (DateTime?)p.LastModified);
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
                    if (result.Products.Any())
                    {
                        foreach (var pDto in result.Products)
                        {
                            var localProduct = await db.Products.FirstOrDefaultAsync(p => p.Id == pDto.Id);
                            if (localProduct == null)
                            {
                                db.Products.Add(new Product { Id = pDto.Id, InternalCode = pDto.InternalCode, Barcode = pDto.Barcode, Name = pDto.Name, Price = pDto.Price, Stock = pDto.Stock, IsActive = pDto.IsActive, LastModified = pDto.LastModified, CreationDate = DateTime.Now });
                            }
                            else
                            {
                                localProduct.InternalCode = pDto.InternalCode; localProduct.Barcode = pDto.Barcode; localProduct.Name = pDto.Name; localProduct.Price = pDto.Price; localProduct.Stock = pDto.Stock; localProduct.IsActive = pDto.IsActive; localProduct.LastModified = pDto.LastModified;
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
                            db.PaymentMethods.Add(new PaymentMethod { Id = pm.Id, Name = pm.Name, IsActive = pm.IsActive, DiscountPercentage = pm.DiscountPercentage });
                        }
                    }

                    await db.SaveChangesAsync();
                    dataChanged = true;
                }
            }
            
            // Para simplificar, asumimos que siempre notificamos si hubo conexión exitosa
            OnSyncCompleted?.Invoke();
        }
    }
}

