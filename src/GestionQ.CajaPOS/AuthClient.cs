using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GestionQ.Domain.DTOs;

namespace GestionQ.CajaPOS
{
    public class AuthClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;

        public AuthClient(string serverUrl)
        {
            _serverUrl = serverUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(1.5);
        }

        public async Task<PosLoginResponseDto> LoginAsync(string pin, string posIdentifier)
        {
            try {
                var request = new PosLoginRequestDto { Pin = pin, PosIdentifier = posIdentifier };
                var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/posauth/login", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PosLoginResponseDto>() ?? new PosLoginResponseDto { Success = false };
            } catch {
                using var db = new LocalDbContext();
                var usersSetting = db.SystemSettings.FirstOrDefault(s => s.Key == "PosUsers");
                if (usersSetting != null && !string.IsNullOrEmpty(usersSetting.Value)) {
                    var users = System.Text.Json.JsonSerializer.Deserialize<List<PosUserSyncDto>>(usersSetting.Value);
                    var user = users?.FirstOrDefault(u => u.Pin == pin);
                    if (user != null) {
                        return new PosLoginResponseDto { Success = true, UserId = user.Id, UserName = user.UserName, FullName = user.FullName };
                    }
                    return new PosLoginResponseDto { Success = false, ErrorMessage = "PIN incorrecto (Modo Fuera de Línea)." };
                }
                return new PosLoginResponseDto { Success = false, ErrorMessage = "Servidor caído y no hay usuarios en caché local. Conéctese a internet al menos una vez." };
            }
        }

        public async Task<PosOpenRegisterResponseDto> OpenRegisterAsync(string userId, string posIdentifier, decimal initialBalance)
        {
            // OFFLINE FIRST: Siempre creamos la caja local. SyncWorker la enviará al servidor.
            using var db = new LocalDbContext();
            var newReg = new OfflineCashRegister {
                UserId = userId,
                InitialBalance = initialBalance,
                OpeningDate = DateTime.Now
            };
            db.OfflineCashRegisters.Add(newReg);
            db.SaveChanges();
            return new PosOpenRegisterResponseDto { Success = true, CashRegisterId = newReg.Id };
        }

        public async Task<(bool success, string errorMessage)> AddMovementAsync(int cashRegisterId, decimal amount, string description, string type)
        {
            try {
                using var db = new LocalDbContext();
                db.Movements.Add(new GestionQ.Domain.Entities.CashRegisterMovement {
                    GlobalId = Guid.NewGuid(),
                    Amount = amount,
                    Description = description,
                    Type = type,
                    CashRegisterId = cashRegisterId,
                    Date = DateTime.Now,
                    IsSynced = false
                });
                db.SaveChanges();
                return (true, "");
            } catch (Exception ex) {
                return (false, "Error al guardar offline: " + ex.Message);
            }
        }

        public async Task<bool> CloseRegisterAsync(int cashRegisterId, decimal finalBalance)
        {
            using var db = new LocalDbContext();
            var offlineReg = db.OfflineCashRegisters.FirstOrDefault(r => r.Id == cashRegisterId);
            if (offlineReg != null) {
                offlineReg.ClosingDate = DateTime.Now;
                offlineReg.FinalCashBalance = finalBalance;
                offlineReg.IsSynced = false;
                db.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<PosStatusResponseDto> GetStatusAsync(string posIdentifier)
        {
            // OFFLINE FIRST: Siempre revisamos si hay una caja local abierta.
            using var db = new LocalDbContext();
            var offlineReg = db.OfflineCashRegisters.FirstOrDefault(r => r.ClosingDate == null);
            if (offlineReg != null) {
                var usersSetting = db.SystemSettings.FirstOrDefault(s => s.Key == "PosUsers");
                var users = usersSetting != null ? System.Text.Json.JsonSerializer.Deserialize<List<PosUserSyncDto>>(usersSetting.Value) : null;
                var user = users?.FirstOrDefault(u => u.Id == offlineReg.UserId);

                return new PosStatusResponseDto {
                    HasOpenRegister = true,
                    CashRegisterId = offlineReg.Id,
                    UserId = offlineReg.UserId,
                    UserName = user?.UserName,
                    FullName = user?.FullName
                };
            }
            return new PosStatusResponseDto { HasOpenRegister = false };
        }

        public async Task<string> GetRegisterTicketAsync(int cashRegisterId)
        {
            using var db = new LocalDbContext();
            var offlineReg = db.OfflineCashRegisters.FirstOrDefault(r => r.Id == cashRegisterId);
            if (offlineReg == null) return "Caja no encontrada.";

            // Generar localmente SIEMPRE para asegurar que los totales incluyan ventas recién hechas que aún no se sincronizan
            var sales = db.Sales.Where(s => s.CashRegisterId == offlineReg.Id && !s.IsCancelled).ToList();
            var movements = db.Movements.Where(m => m.CashRegisterId == offlineReg.Id).ToList();
            
            // Los pagos no están directamente enlazados, así que los sacamos a través de las ventas
            var saleIds = sales.Select(s => s.Id).ToList();
            var payments = db.SalePayments.Where(p => saleIds.Contains(p.SaleId)).ToList();
            
            var usersSetting = db.SystemSettings.FirstOrDefault(s => s.Key == "PosUsers");
            var users = usersSetting != null ? System.Text.Json.JsonSerializer.Deserialize<List<PosUserSyncDto>>(usersSetting.Value) : null;
            var user = users?.FirstOrDefault(u => u.Id == offlineReg.UserId);
            var userName = user?.UserName ?? offlineReg.UserId;

            string CenterText(string text, int width = 42)
            {
                if (string.IsNullOrEmpty(text)) return "";
                text = text.Trim();
                if (text.Length >= width) return text.Substring(0, width);
                int spaces = (width - text.Length) / 2;
                return text.PadLeft(text.Length + spaces).PadRight(width);
            }

            string FormatLine(string leftText, string rightText, int width = 42)
            {
                leftText = leftText ?? "";
                rightText = rightText ?? "";
                if (leftText.Length + rightText.Length > width)
                {
                    int maxLeft = width - rightText.Length - 1;
                    if (maxLeft > 0) leftText = leftText.Substring(0, maxLeft);
                    else leftText = "";
                }
                return leftText.PadRight(width - rightText.Length) + rightText;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(FormatLine("Fecha/hora de impresión:", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            sb.AppendLine();
            
            int posNumber = 1;
            var posNumSetting = db.SystemSettings.FirstOrDefault(s => s.Key == "PosNumber");
            if (posNumSetting != null && int.TryParse(posNumSetting.Value, out int parsed)) {
                posNumber = parsed;
            }
            
            if (offlineReg.ClosingDate.HasValue) {
                sb.AppendLine(CenterText($"CIERRE DE CAJA #{posNumber:D5}"));
                sb.AppendLine(CenterText("INFORME Z"));
            } else {
                sb.AppendLine(CenterText($"PARCIAL DE CAJA #{posNumber:D5}"));
                sb.AppendLine(CenterText("INFORME X"));
            }
            sb.AppendLine(new string('-', 42));
            sb.AppendLine(FormatLine("Cajero:", userName));
            sb.AppendLine(FormatLine("Apertura:", offlineReg.OpeningDate.ToString("dd/MM/yyyy HH:mm")));
            if (offlineReg.ClosingDate.HasValue)
                sb.AppendLine(FormatLine("Cierre:", offlineReg.ClosingDate.Value.ToString("dd/MM/yyyy HH:mm")));
            
            sb.AppendLine(new string('-', 42));
            sb.AppendLine(FormatLine("SALDO INICIAL:", $"$ {offlineReg.InitialBalance:N2}"));
            
            decimal totalSales = sales.Sum(s => s.TotalAmount);
            var incomes = movements.Where(m => m.Type == "Ingreso").Sum(m => m.Amount);
            var expenses = movements.Where(m => m.Type == "Egreso").Sum(m => m.Amount);
            
            sb.AppendLine();
            sb.AppendLine(CenterText("Resumen de operaciones", 42).Replace(" ", "-").Replace("Resumen-de-operaciones", " Resumen de operaciones "));
            sb.AppendLine(FormatLine("VENTAS", $"$ {totalSales:N2}"));
            sb.AppendLine(FormatLine("INGRESOS (Movi)", $"$ {incomes:N2}"));
            sb.AppendLine(FormatLine("RETIROS (Movi)", $"$ {expenses:N2}"));
            sb.AppendLine(FormatLine("Cant. Operaciones", (sales.Count + movements.Count).ToString()));
            
            decimal expected = offlineReg.InitialBalance + incomes - expenses;
            var cashPayments = payments.Where(p => p.PaymentMethodId == 1).Sum(p => p.Amount);
            expected += cashPayments;
            
            sb.AppendLine();
            sb.AppendLine(CenterText("Medios de cobro en caja", 42).Replace(" ", "-").Replace("Medios-de-cobro-en-caja", " Medios de cobro en caja "));
            
            var paymentSummary = payments.GroupBy(p => p.PaymentMethodId)
                .Select(g => new { MethodId = g.Key, Total = g.Sum(p => p.Amount) })
                .ToList();

            var paymentMethodNames = db.PaymentMethods.ToDictionary(pm => pm.Id, pm => pm.Name);
            
            foreach (var p in paymentSummary)
            {
                string methodName = paymentMethodNames.ContainsKey(p.MethodId) ? paymentMethodNames[p.MethodId] : $"Metodo {p.MethodId}";
                if (methodName.ToUpper() == "EFECTIVO") continue;
                sb.AppendLine(FormatLine(methodName, p.Total.ToString("N2").PadRight(15) + "$ 0,00"));
            }
            
            if (offlineReg.FinalCashBalance.HasValue) {
                sb.AppendLine(FormatLine("EFECTIVO (Esperado / Real)", expected.ToString("N2").PadRight(15) + offlineReg.FinalCashBalance.Value.ToString("N2")));
                sb.AppendLine(new string('-', 42));
                sb.AppendLine(FormatLine("SALDO (Diferencia Efectivo)", $"$ {(offlineReg.FinalCashBalance.Value - expected):N2}"));
            } else {
                sb.AppendLine(FormatLine("EFECTIVO (Esperado)", expected.ToString("N2")));
                sb.AppendLine(new string('-', 42));
            }
            
            if (!offlineReg.ServerCashRegisterId.HasValue) {
                sb.AppendLine();
                sb.AppendLine(CenterText("* Nota: Caja Offline (No sincronizada) *"));
            }
            return sb.ToString();
        }
    }
}
