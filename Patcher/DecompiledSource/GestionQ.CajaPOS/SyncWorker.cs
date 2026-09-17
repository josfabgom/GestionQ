using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionQ.CajaPOS;

public class SyncWorker
{
	private readonly HttpClient _httpClient;

	private readonly string _serverUrl;

	private CancellationTokenSource _cts = new CancellationTokenSource();

	public string PosIdentifier = Environment.MachineName;

	public event Action OnSyncCompleted;

	public event Action<string> OnSyncError;

	public SyncWorker(string serverUrl)
	{
		_serverUrl = serverUrl.TrimEnd('/');
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromSeconds(30L);
	}

	public void Start()
	{
		_cts = new CancellationTokenSource();
		Task.Run(async delegate
		{
			await RunLoopAsync(_cts.Token);
		});
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
				this.OnSyncError?.Invoke(ex.Message);
			}
			await Task.Delay(TimeSpan.FromMinutes(1L), token);
		}
	}

	public async Task PerformSyncAsync()
	{
		LocalDbContext db = new LocalDbContext();
		try
		{
			List<Sale> unsyncedSales = await (from s in db.Sales.Include((Sale s) => s.Items).Include((Sale s) => s.Payments)
				where !s.IsSynced
				select s).ToListAsync();
			List<CashRegisterMovement> unsyncedMovements = await db.Movements.Where((CashRegisterMovement m) => !m.IsSynced).ToListAsync();
			List<Customer> unsyncedCustomers = await db.Customers.Where((Customer c) => EF.Property<bool>(c, "IsSynced") == false).ToListAsync();
			List<OfflineCashRegister> unsyncedRegisters = await db.OfflineCashRegisters.Where((OfflineCashRegister r) => !r.IsSynced).ToListAsync();
			List<OfflineCashRegister> allOfflineRegisters = await db.OfflineCashRegisters.ToListAsync();
			if (unsyncedSales.Any() || unsyncedMovements.Any() || unsyncedCustomers.Any() || unsyncedRegisters.Any())
			{
				SyncPushRequest value = new SyncPushRequest
				{
					PosIdentifier = PosIdentifier,
					OfflineCashRegisters = unsyncedRegisters.Select((OfflineCashRegister r) => new OfflineCashRegisterSyncDto
					{
						GlobalId = r.GlobalId,
						UserId = r.UserId,
						OpeningDate = r.OpeningDate,
						ClosingDate = r.ClosingDate,
						InitialBalance = r.InitialBalance,
						FinalCashBalance = r.FinalCashBalance
					}).ToList(),
					NewCustomers = unsyncedCustomers.Select((Customer c) => new CustomerSyncDto
					{
						Dni = c.Dni,
						Name = c.Name,
						Email = c.Email,
						Phone = c.Phone,
						Cuit = c.Cuit
					}).ToList(),
					Sales = unsyncedSales.Select(delegate(Sale s)
					{
						Customer customer = (s.CustomerId.HasValue ? db.Customers.Find(s.CustomerId.Value) : null);
						OfflineCashRegister offlineCashRegister2 = allOfflineRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == s.CashRegisterId);
						return new SaleSyncDto
						{
							GlobalId = s.GlobalId,
							Date = s.Date,
							TotalAmount = s.TotalAmount,
							SubTotal = s.SubTotal,
							DiscountAmount = s.DiscountAmount,
							PaymentDiscountAmount = s.PaymentDiscountAmount,
							UserId = s.UserId,
							CashRegisterId = ((offlineCashRegister2 == null) ? s.CashRegisterId : offlineCashRegister2.ServerCashRegisterId),
							OfflineCashRegisterGlobalId = offlineCashRegister2?.GlobalId,
							CustomerDni = customer?.Dni,
							RequestElectronicInvoice = s.RequestElectronicInvoice,
							IsCancelled = s.IsCancelled,
							CancellationDate = s.CancellationDate,
							Items = s.Items.Select((SaleItem i) => new SaleItemSyncDto
							{
								ProductId = i.ProductId,
								Quantity = i.Quantity,
								UnitPrice = i.UnitPrice,
								DiscountAmount = i.DiscountAmount
							}).ToList(),
							Payments = s.Payments.Select((SalePayment p) => new SalePaymentSyncDto
							{
								PaymentMethodId = p.PaymentMethodId,
								Amount = p.Amount,
								TransactionReference = p.TransactionReference
							}).ToList()
						};
					}).ToList(),
					Movements = unsyncedMovements.Select(delegate(CashRegisterMovement m)
					{
						OfflineCashRegister offlineCashRegister = allOfflineRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == m.CashRegisterId);
						return new MovementSyncDto
						{
							GlobalId = m.GlobalId,
							Amount = m.Amount,
							Type = m.Type,
							Description = m.Description,
							Date = m.Date,
							CashRegisterId = ((offlineCashRegister == null) ? new int?(m.CashRegisterId) : offlineCashRegister.ServerCashRegisterId),
							OfflineCashRegisterGlobalId = offlineCashRegister?.GlobalId
						};
					}).ToList()
				};
				HttpResponseMessage httpResponseMessage = await _httpClient.PostAsJsonAsync(_serverUrl + "/api/sync/push", value);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					try
					{
						SyncPushResponse syncPushResponse = await httpResponseMessage.Content.ReadFromJsonAsync<SyncPushResponse>();
						if (syncPushResponse?.RegisterIdMap != null)
						{
							foreach (OfflineCashRegister item in unsyncedRegisters)
							{
								if (syncPushResponse.RegisterIdMap.TryGetValue(item.GlobalId, out var value2))
								{
									item.ServerCashRegisterId = value2;
								}
							}
						}
					}
					catch
					{
					}
					foreach (Sale item2 in unsyncedSales)
					{
						item2.IsSynced = true;
					}
					foreach (CashRegisterMovement item3 in unsyncedMovements)
					{
						item3.IsSynced = true;
					}
					foreach (OfflineCashRegister item4 in unsyncedRegisters)
					{
						item4.IsSynced = true;
					}
					foreach (Customer item5 in unsyncedCustomers)
					{
						db.Entry(item5).Property("IsSynced").CurrentValue = true;
					}
					await db.SaveChangesAsync();
				}
			}
			DateTime? lastSyncDate = await ((IQueryable<Product>)db.Products).MaxAsync((Expression<Func<Product, DateTime?>>)((Product p) => p.LastModified), default(CancellationToken));
			if (await db.Products.AnyAsync((Product p) => p.ImageUrl == null))
			{
				lastSyncDate = null;
			}
			SyncPullRequest value3 = new SyncPullRequest
			{
				LastSyncDate = lastSyncDate,
				PosIdentifier = PosIdentifier
			};
			HttpResponseMessage httpResponseMessage2 = await _httpClient.PostAsJsonAsync(_serverUrl + "/api/sync/pull", value3);
			if (httpResponseMessage2.IsSuccessStatusCode)
			{
				SyncPullResponse result = await httpResponseMessage2.Content.ReadFromJsonAsync<SyncPullResponse>();
				if (result != null)
				{
					if (result.CompanyInfo != null)
					{
						List<SystemSetting> entities = await db.SystemSettings.ToListAsync();
						db.SystemSettings.RemoveRange(entities);
						db.SystemSettings.Add(new SystemSetting
						{
							Key = "CompanyName",
							Value = result.CompanyInfo.Name
						});
						db.SystemSettings.Add(new SystemSetting
						{
							Key = "CompanyLogoUrl",
							Value = result.CompanyInfo.LogoUrl
						});
						if (!string.IsNullOrEmpty(result.CompanyInfo.LogoUrl))
						{
							await DownloadImageAsync(result.CompanyInfo.LogoUrl);
						}
					}
					if (result.Users != null && result.Users.Any())
					{
						SystemSetting systemSetting = await db.SystemSettings.FirstOrDefaultAsync((SystemSetting s) => s.Key == "PosUsers");
						string value4 = JsonSerializer.Serialize(result.Users);
						if (systemSetting == null)
						{
							db.SystemSettings.Add(new SystemSetting
							{
								Key = "PosUsers",
								Value = value4
							});
						}
						else
						{
							systemSetting.Value = value4;
						}
					}
					if (result.Products.Any())
					{
						foreach (ProductSyncDto pDto in result.Products)
						{
							Product product = await db.Products.FirstOrDefaultAsync((Product p) => p.Id == pDto.Id);
							if (product == null)
							{
								db.Products.Add(new Product
								{
									Id = pDto.Id,
									InternalCode = pDto.InternalCode,
									Barcode = pDto.Barcode,
									Name = pDto.Name,
									Price = pDto.Price,
									Stock = pDto.Stock,
									IsActive = pDto.IsActive,
									LastModified = pDto.LastModified,
									CreationDate = DateTime.Now,
									ImageUrl = pDto.ImageUrl
								});
							}
							else
							{
								product.InternalCode = pDto.InternalCode;
								product.Barcode = pDto.Barcode;
								product.Name = pDto.Name;
								product.Price = pDto.Price;
								product.Stock = pDto.Stock;
								product.IsActive = pDto.IsActive;
								product.LastModified = pDto.LastModified;
								product.ImageUrl = pDto.ImageUrl;
							}
							if (!string.IsNullOrEmpty(pDto.ImageUrl))
							{
								await DownloadImageAsync(pDto.ImageUrl);
							}
						}
					}
					if (result.Customers.Any())
					{
						List<Customer> entities2 = await db.Customers.ToListAsync();
						db.Customers.RemoveRange(entities2);
						foreach (CustomerSyncDto customer2 in result.Customers)
						{
							db.Customers.Add(new Customer
							{
								Id = customer2.Id,
								Name = customer2.Name,
								Email = customer2.Email,
								Phone = customer2.Phone,
								Dni = customer2.Dni,
								Cuit = customer2.Cuit,
								IsActive = customer2.IsActive
							});
						}
					}
					if (result.Departments.Any())
					{
						List<Department> entities3 = await db.Departments.ToListAsync();
						db.Departments.RemoveRange(entities3);
						foreach (DepartmentSyncDto department in result.Departments)
						{
							db.Departments.Add(new Department
							{
								Id = department.Id,
								Name = department.Name,
								Hotkey = department.Hotkey,
								VirtualProductId = department.VirtualProductId,
								VatRateId = 1
							});
						}
					}
					if (result.PaymentMethods.Any())
					{
						List<PaymentMethod> entities4 = await db.PaymentMethods.ToListAsync();
						db.PaymentMethods.RemoveRange(entities4);
						foreach (PaymentMethodSyncDto paymentMethod in result.PaymentMethods)
						{
							db.PaymentMethods.Add(new PaymentMethod
							{
								Id = paymentMethod.Id,
								Name = paymentMethod.Name,
								IsActive = paymentMethod.IsActive,
								DiscountPercentage = paymentMethod.DiscountPercentage,
								DiscountValidFrom = paymentMethod.DiscountValidFrom,
								DiscountValidTo = paymentMethod.DiscountValidTo
							});
						}
					}
					if (result.ActivePromotions != null)
					{
						SystemSetting systemSetting2 = await db.SystemSettings.FirstOrDefaultAsync((SystemSetting s) => s.Key == "ActivePromotions");
						string value5 = (result.ActivePromotions.Any() ? JsonSerializer.Serialize(result.ActivePromotions) : "[]");
						if (systemSetting2 == null)
						{
							db.SystemSettings.Add(new SystemSetting
							{
								Key = "ActivePromotions",
								Value = value5
							});
						}
						else
						{
							systemSetting2.Value = value5;
						}
					}
					await db.SaveChangesAsync();
				}
			}
			this.OnSyncCompleted?.Invoke();
		}
		finally
		{
			if (db != null)
			{
				((IDisposable)db).Dispose();
			}
		}
	}

	private async Task DownloadImageAsync(string relativeUrl)
	{
		_ = 1;
		try
		{
			string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeUrl.TrimStart('/'));
			string directoryName = Path.GetDirectoryName(localPath);
			if (directoryName != null && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			if (relativeUrl.Contains("logo.png") || !File.Exists(localPath))
			{
				string requestUri = _serverUrl + "/" + relativeUrl.TrimStart('/');
				await File.WriteAllBytesAsync(localPath, await _httpClient.GetByteArrayAsync(requestUri));
			}
		}
		catch
		{
		}
	}
}
