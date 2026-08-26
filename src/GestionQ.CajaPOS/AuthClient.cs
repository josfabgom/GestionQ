using System;
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
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<PosLoginResponseDto> LoginAsync(string pin, string posIdentifier)
        {
            var request = new PosLoginRequestDto { Pin = pin, PosIdentifier = posIdentifier };
            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/posauth/login", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PosLoginResponseDto>();
        }

        public async Task<PosOpenRegisterResponseDto> OpenRegisterAsync(string userId, string posIdentifier, decimal initialBalance)
        {
            var request = new PosOpenRegisterRequestDto
            {
                UserId = userId,
                PosIdentifier = posIdentifier,
                InitialBalance = initialBalance
            };
            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/posauth/open-register", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PosOpenRegisterResponseDto>();
        }

        public async Task<(bool success, string errorMessage)> AddMovementAsync(int cashRegisterId, decimal amount, string description, string type)
        {
            var request = new
            {
                CashRegisterId = cashRegisterId,
                Amount = amount,
                Description = description,
                Type = type
            };
            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/posauth/add-movement", request);
            
            if (!response.IsSuccessStatusCode)
            {
                try 
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (errorResult.TryGetProperty("error", out var errorMsg))
                    {
                        return (false, errorMsg.GetString() ?? "Error desconocido en el servidor.");
                    }
                } 
                catch { }
                return (false, "Error de conexión con el servidor central.");
            }

            return (true, "");
        }

        public async Task<bool> CloseRegisterAsync(int cashRegisterId, decimal finalBalance)
        {
            var request = new PosCloseRegisterRequestDto
            {
                CashRegisterId = cashRegisterId,
                FinalCashBalance = finalBalance
            };
            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/posauth/close-register", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            return result.GetProperty("success").GetBoolean();
        }

        public async Task<PosStatusResponseDto> GetStatusAsync(string posIdentifier)
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/posauth/status/{posIdentifier}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PosStatusResponseDto>();
        }

        public async Task<string> GetRegisterTicketAsync(int cashRegisterId)
        {
            var response = await _httpClient.GetAsync($"{_serverUrl}/api/posauth/print-register/{cashRegisterId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            return result.GetProperty("ticketText").GetString() ?? "";
        }
    }
}
