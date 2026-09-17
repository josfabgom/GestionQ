using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GestionQ.Domain.DTOs;
using GestionQ.Domain.Entities;

namespace GestionQ.CajaPOS;

public class AuthClient
{
	private readonly HttpClient _httpClient;

	private readonly string _serverUrl;

	public AuthClient(string serverUrl)
	{
		_serverUrl = serverUrl.TrimEnd('/');
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromSeconds(5L);
	}

	public async Task<PosLoginResponseDto> LoginAsync(string pin, string posIdentifier)
	{
		string pin2 = pin;
		try
		{
			PosLoginRequestDto value = new PosLoginRequestDto
			{
				Pin = pin2,
				PosIdentifier = posIdentifier
			};
			HttpResponseMessage obj = await _httpClient.PostAsJsonAsync(_serverUrl + "/api/posauth/login", value);
			obj.EnsureSuccessStatusCode();
			return (await obj.Content.ReadFromJsonAsync<PosLoginResponseDto>()) ?? new PosLoginResponseDto
			{
				Success = false
			};
		}
		catch
		{
			using LocalDbContext localDbContext = new LocalDbContext();
			SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosUsers");
			if (systemSetting != null && !string.IsNullOrEmpty(systemSetting.Value))
			{
				PosUserSyncDto posUserSyncDto = JsonSerializer.Deserialize<List<PosUserSyncDto>>(systemSetting.Value)?.FirstOrDefault((PosUserSyncDto u) => u.Pin == pin2);
				if (posUserSyncDto != null)
				{
					return new PosLoginResponseDto
					{
						Success = true,
						UserId = posUserSyncDto.Id,
						UserName = posUserSyncDto.UserName,
						FullName = posUserSyncDto.FullName
					};
				}
				return new PosLoginResponseDto
				{
					Success = false,
					ErrorMessage = "PIN incorrecto (Modo Fuera de Línea)."
				};
			}
			return new PosLoginResponseDto
			{
				Success = false,
				ErrorMessage = "Servidor caído y no hay usuarios en caché local. Conéctese a internet al menos una vez."
			};
		}
	}

	public async Task<PosOpenRegisterResponseDto> OpenRegisterAsync(string userId, string posIdentifier, decimal initialBalance)
	{
		using LocalDbContext localDbContext = new LocalDbContext();
		OfflineCashRegister offlineCashRegister = new OfflineCashRegister
		{
			UserId = userId,
			InitialBalance = initialBalance,
			OpeningDate = DateTime.Now
		};
		localDbContext.OfflineCashRegisters.Add(offlineCashRegister);
		localDbContext.SaveChanges();
		return new PosOpenRegisterResponseDto
		{
			Success = true,
			CashRegisterId = offlineCashRegister.Id
		};
	}

	public async Task<(bool success, string errorMessage)> AddMovementAsync(int cashRegisterId, decimal amount, string description, string type)
	{
		try
		{
			using LocalDbContext localDbContext = new LocalDbContext();
			localDbContext.Movements.Add(new CashRegisterMovement
			{
				GlobalId = Guid.NewGuid(),
				Amount = amount,
				Description = description,
				Type = type,
				CashRegisterId = cashRegisterId,
				Date = DateTime.Now,
				IsSynced = false
			});
			localDbContext.SaveChanges();
			return (success: true, errorMessage: "");
		}
		catch (Exception ex)
		{
			return (success: false, errorMessage: "Error al guardar offline: " + ex.Message);
		}
	}

	public async Task<bool> CloseRegisterAsync(int cashRegisterId, decimal finalBalance)
	{
		using LocalDbContext localDbContext = new LocalDbContext();
		OfflineCashRegister offlineCashRegister = localDbContext.OfflineCashRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == cashRegisterId);
		if (offlineCashRegister != null)
		{
			offlineCashRegister.ClosingDate = DateTime.Now;
			offlineCashRegister.FinalCashBalance = finalBalance;
			offlineCashRegister.IsSynced = false;
			localDbContext.SaveChanges();
			return true;
		}
		return false;
	}

	public async Task<PosStatusResponseDto> GetStatusAsync(string posIdentifier)
	{
		using LocalDbContext localDbContext = new LocalDbContext();
		OfflineCashRegister offlineReg = localDbContext.OfflineCashRegisters.FirstOrDefault((OfflineCashRegister r) => r.ClosingDate == null);
		if (offlineReg != null)
		{
			SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosUsers");
			PosUserSyncDto posUserSyncDto = ((systemSetting != null) ? JsonSerializer.Deserialize<List<PosUserSyncDto>>(systemSetting.Value) : null)?.FirstOrDefault((PosUserSyncDto u) => u.Id == offlineReg.UserId);
			return new PosStatusResponseDto
			{
				HasOpenRegister = true,
				CashRegisterId = offlineReg.Id,
				UserId = offlineReg.UserId,
				UserName = posUserSyncDto?.UserName,
				FullName = posUserSyncDto?.FullName
			};
		}
		return new PosStatusResponseDto
		{
			HasOpenRegister = false
		};
	}

	public async Task<string> GetRegisterTicketAsync(int cashRegisterId)
	{
		using (LocalDbContext localDbContext = new LocalDbContext())
		{
			OfflineCashRegister offlineReg = localDbContext.OfflineCashRegisters.FirstOrDefault((OfflineCashRegister r) => r.Id == cashRegisterId);
			if (offlineReg == null)
			{
				return "Caja no encontrada.";
			}
			List<Sale> list = localDbContext.Sales.Where((Sale s) => s.CashRegisterId == (int?)offlineReg.Id && !s.IsCancelled).ToList();
			List<CashRegisterMovement> list2 = localDbContext.Movements.Where((CashRegisterMovement m) => m.CashRegisterId == offlineReg.Id).ToList();
			List<int> saleIds = list.Select((Sale s) => s.Id).ToList();
			List<SalePayment> source = localDbContext.SalePayments.Where((SalePayment p) => saleIds.Contains(p.SaleId)).ToList();
			SystemSetting systemSetting = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosUsers");
			string rightText2 = (((systemSetting != null) ? JsonSerializer.Deserialize<List<PosUserSyncDto>>(systemSetting.Value) : null)?.FirstOrDefault((PosUserSyncDto u) => u.Id == offlineReg.UserId))?.UserName ?? offlineReg.UserId;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(FormatLine("Fecha/hora de impresión:", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
			stringBuilder.AppendLine();
			int value = 1;
			SystemSetting systemSetting2 = localDbContext.SystemSettings.FirstOrDefault((SystemSetting s) => s.Key == "PosNumber");
			if (systemSetting2 != null && int.TryParse(systemSetting2.Value, out var result))
			{
				value = result;
			}
			if (offlineReg.ClosingDate.HasValue)
			{
				stringBuilder.AppendLine(CenterText($"CIERRE DE CAJA #{value:D5}"));
				stringBuilder.AppendLine(CenterText("INFORME Z"));
			}
			else
			{
				stringBuilder.AppendLine(CenterText($"PARCIAL DE CAJA #{value:D5}"));
				stringBuilder.AppendLine(CenterText("INFORME X"));
			}
			stringBuilder.AppendLine(new string('-', 42));
			stringBuilder.AppendLine(FormatLine("Cajero:", rightText2));
			stringBuilder.AppendLine(FormatLine("Apertura:", offlineReg.OpeningDate.ToString("dd/MM/yyyy HH:mm")));
			if (offlineReg.ClosingDate.HasValue)
			{
				stringBuilder.AppendLine(FormatLine("Cierre:", offlineReg.ClosingDate.Value.ToString("dd/MM/yyyy HH:mm")));
			}
			stringBuilder.AppendLine(new string('-', 42));
			stringBuilder.AppendLine(FormatLine("SALDO INICIAL:", $"$ {offlineReg.InitialBalance:N2}"));
			decimal value2 = list.Sum((Sale s) => s.TotalAmount);
			decimal num = list2.Where((CashRegisterMovement m) => m.Type == "Ingreso").Sum((CashRegisterMovement m) => m.Amount);
			decimal num2 = list2.Where((CashRegisterMovement m) => m.Type == "Egreso").Sum((CashRegisterMovement m) => m.Amount);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(CenterText("Resumen de operaciones").Replace(" ", "-").Replace("Resumen-de-operaciones", " Resumen de operaciones "));
			stringBuilder.AppendLine(FormatLine("VENTAS", $"$ {value2:N2}"));
			stringBuilder.AppendLine(FormatLine("INGRESOS (Movi)", $"$ {num:N2}"));
			stringBuilder.AppendLine(FormatLine("RETIROS (Movi)", $"$ {num2:N2}"));
			stringBuilder.AppendLine(FormatLine("Cant. Operaciones", (list.Count + list2.Count).ToString()));
			decimal num3 = offlineReg.InitialBalance + num - num2;
			decimal num4 = source.Where((SalePayment p) => p.PaymentMethodId == 1).Sum((SalePayment p) => p.Amount);
			num3 += num4;
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(CenterText("Medios de cobro en caja").Replace(" ", "-").Replace("Medios-de-cobro-en-caja", " Medios de cobro en caja "));
			var list3 = (from p in source
				group p by p.PaymentMethodId into g
				select new
				{
					MethodId = g.Key,
					Total = g.Sum((SalePayment p) => p.Amount)
				}).ToList();
			Dictionary<int, string> dictionary = localDbContext.PaymentMethods.ToDictionary((PaymentMethod pm) => pm.Id, (PaymentMethod pm) => pm.Name);
			foreach (var item in list3)
			{
				string text2 = (dictionary.ContainsKey(item.MethodId) ? dictionary[item.MethodId] : $"Metodo {item.MethodId}");
				if (!(text2.ToUpper() == "EFECTIVO"))
				{
					stringBuilder.AppendLine(FormatLine(text2, item.Total.ToString("N2").PadRight(15) + "$ 0,00"));
				}
			}
			if (offlineReg.FinalCashBalance.HasValue)
			{
				stringBuilder.AppendLine(FormatLine("EFECTIVO (Esperado / Real)", num3.ToString("N2").PadRight(15) + offlineReg.FinalCashBalance.Value.ToString("N2")));
				stringBuilder.AppendLine(new string('-', 42));
				stringBuilder.AppendLine(FormatLine("SALDO (Diferencia Efectivo)", $"$ {offlineReg.FinalCashBalance.Value - num3:N2}"));
			}
			else
			{
				stringBuilder.AppendLine(FormatLine("EFECTIVO (Esperado)", num3.ToString("N2")));
				stringBuilder.AppendLine(new string('-', 42));
			}
			if (!offlineReg.ServerCashRegisterId.HasValue)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine(CenterText("* Nota: Caja Offline (No sincronizada) *"));
			}
			return stringBuilder.ToString();
		}
		static string CenterText(string text, int width = 42)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			text = text.Trim();
			if (text.Length >= width)
			{
				return text.Substring(0, width);
			}
			int num5 = (width - text.Length) / 2;
			return text.PadLeft(text.Length + num5).PadRight(width);
		}
		static string FormatLine(string leftText, string rightText, int width = 42)
		{
			leftText = leftText ?? "";
			rightText = rightText ?? "";
			if (leftText.Length + rightText.Length > width)
			{
				int num6 = width - rightText.Length - 1;
				leftText = ((num6 <= 0) ? "" : leftText.Substring(0, num6));
			}
			return leftText.PadRight(width - rightText.Length) + rightText;
		}
	}
}
