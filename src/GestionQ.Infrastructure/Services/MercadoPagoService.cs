using GestionQ.Domain.Entities;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace GestionQ.Infrastructure.Services
{
    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _userId; // Could be extracted dynamically from the token, but we'll fetch via API if needed or rely on endpoints that don't need it.

        public MercadoPagoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.mercadopago.com/");
        }

        public async Task<MpOrderResponse> CreateQrOrderAsync(MercadoPagoConfig config, string reference, decimal amount, string description)
        {
            try
            {
                // Dynamic QR endpoint: /instore/orders/qr/seller/collectors/{user_id}/pos/{external_pos_id}/qrs
                // First we need to get the User ID from the token if we don't have it.
                var userId = await GetUserIdFromTokenAsync(config.AccessToken);
                if (userId == null)
                {
                    return new MpOrderResponse { Success = false, ErrorMessage = "No se pudo obtener el User ID del token." };
                }

                var requestUrl = $"instore/orders/qr/seller/collectors/{userId}/pos/{config.ExternalPosId ?? ""}/qrs";

                var payload = new
                {
                    external_reference = reference,
                    title = "Venta GestionQ",
                    description = description,
                    notification_url = "https://www.yourdomain.com/webhook", // Update later
                    total_amount = amount,
                    items = new[]
                    {
                        new
                        {
                            title = "Venta",
                            unit_price = amount,
                            quantity = 1,
                            unit_measure = "unit",
                            total_amount = amount
                        }
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                var contentString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(contentString);
                    var qrData = doc.RootElement.GetProperty("qr_data").GetString();

                    return new MpOrderResponse { Success = true, QrData = qrData };
                }
                else
                {
                    return new MpOrderResponse { Success = false, ErrorMessage = $"Error de MP: {contentString}" };
                }
            }
            catch (Exception ex)
            {
                return new MpOrderResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<MpOrderResponse> CreatePointOrderAsync(MercadoPagoConfig config, string reference, decimal amount, string description)
        {
            try
            {
                string deviceId = config.PointDeviceId;
                if (!deviceId.StartsWith("PAX_A910__") && deviceId.StartsWith("SMARTPOS"))
                {
                    deviceId = "PAX_A910__" + deviceId;
                }

                var requestUrl = $"point/integration-api/devices/{deviceId}/payment-intents";
                
                var payload = new
                {
                    amount = (int)Math.Round(amount * 100m), // La API vieja exige centavos (multiplicar x 100).
                    additional_info = new
                    {
                        external_reference = reference,
                        print_on_terminal = true
                    }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
                request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
                request.Content = JsonContent.Create(payload);

                var response = await _httpClient.SendAsync(request);
                var contentString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(contentString);
                    var intentId = doc.RootElement.GetProperty("id").GetString();
                    return new MpOrderResponse { Success = true, OrderId = intentId };
                }
                else
                {
                    // Intentar un parche automático si devuelve un error relacionado con STANDALONE
                    if (contentString.Contains("STANDALONE") || contentString.Contains("operating_mode"))
                    {
                        using var patchReq = new HttpRequestMessage(new HttpMethod("PATCH"), $"point/integration-api/devices/{deviceId}");
                        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
                        patchReq.Content = new StringContent("{\"operating_mode\": \"PDV\"}", System.Text.Encoding.UTF8, "application/json");
                        await _httpClient.SendAsync(patchReq);
                        
                        // Reintentar el cobro
                        using var retryReq = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                        retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);
                        retryReq.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
                        retryReq.Content = JsonContent.Create(payload);
                        
                        var retryRes = await _httpClient.SendAsync(retryReq);
                        var retryContent = await retryRes.Content.ReadAsStringAsync();
                        
                        if (retryRes.IsSuccessStatusCode)
                        {
                            using var doc = JsonDocument.Parse(retryContent);
                            return new MpOrderResponse { Success = true, OrderId = doc.RootElement.GetProperty("id").GetString() };
                        }
                        return new MpOrderResponse { Success = false, ErrorMessage = $"Error (Tras forzar PDV): {retryContent}" };
                    }
                    
                    return new MpOrderResponse { Success = false, ErrorMessage = $"Error API Antigua: {contentString}" };
                }
            }
            catch (Exception ex)
            {
                return new MpOrderResponse { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<MpPaymentStatusResponse> CheckOrderStatusAsync(MercadoPagoConfig config, string reference)
        {
            try
            {
                // We search for payments with this external_reference
                var requestUrl = $"v1/payments/search?external_reference={reference}";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AccessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(contentString);
                    var results = doc.RootElement.GetProperty("results");

                    if (results.GetArrayLength() > 0)
                    {
                        var payment = results[0];
                        var status = payment.GetProperty("status").GetString();

                        if (status == "approved")
                        {
                            return new MpPaymentStatusResponse { IsPaid = true, Status = status };
                        }
                        return new MpPaymentStatusResponse { IsPaid = false, Status = status };
                    }
                }
                
                return new MpPaymentStatusResponse { IsPaid = false, Status = "pending" };
            }
            catch
            {
                return new MpPaymentStatusResponse { IsPaid = false, Status = "error" };
            }
        }

        private async Task<string> GetUserIdFromTokenAsync(string accessToken)
        {
            try
            {
                var requestUrl = "users/me";
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(contentString);
                    return doc.RootElement.GetProperty("id").GetInt64().ToString();
                }
            }
            catch { }
            return null;
        }

        public async Task<(bool Success, string ErrorMessage)> RefundPaymentAsync(string paymentId, string accessToken)
        {
            try
            {
                // Verify the payment to get the actual payment ID if the provided string is an external reference
                // If it's already a numeric payment ID, this search might not return it as external_reference.
                // Assuming paymentId passed is the actual MercadoPago Payment ID (a number) because the refund endpoint requires it.
                // If the stored reference is external_reference, we must fetch the payment first.
                long actualPaymentId = 0;
                if (!long.TryParse(paymentId, out actualPaymentId))
                {
                    // It's probably an external reference, let's search for it
                    var requestUrlSearch = $"v1/payments/search?external_reference={paymentId}";
                    using var requestSearch = new HttpRequestMessage(HttpMethod.Get, requestUrlSearch);
                    requestSearch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var responseSearch = await _httpClient.SendAsync(requestSearch);
                    if (responseSearch.IsSuccessStatusCode)
                    {
                        var contentStringSearch = await responseSearch.Content.ReadAsStringAsync();
                        using var docSearch = JsonDocument.Parse(contentStringSearch);
                        var results = docSearch.RootElement.GetProperty("results");
                        if (results.GetArrayLength() > 0)
                        {
                            actualPaymentId = results[0].GetProperty("id").GetInt64();
                        }
                    }
                }
                else
                {
                    actualPaymentId = long.Parse(paymentId);
                }

                if (actualPaymentId == 0)
                {
                    return (false, "No se encontró el pago original en Mercado Pago asociado a esa referencia.");
                }

                // Call refund API
                var requestUrl = $"v1/payments/{actualPaymentId}/refunds";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
                // An empty object is required by MP for a total refund
                request.Content = JsonContent.Create(new { });

                var response = await _httpClient.SendAsync(request);
                var contentString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    return (true, string.Empty);
                }
                
                // If it's already refunded, it might return 400 with a specific error
                using var doc = JsonDocument.Parse(contentString);
                var message = doc.RootElement.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : contentString;
                
                if (message != null && message.Contains("Unauthorized use of live credentials"))
                {
                    message = "Estás usando credenciales de producción (APP_USR) pero tu aplicación de Mercado Pago no tiene los permisos para operar en vivo ('Go Live'), o estás mezclando pagos de prueba con credenciales reales.";
                }

                return (false, $"Error de MP al devolver: {message}");
            }
            catch (Exception ex)
            {
                return (false, $"Excepción al procesar la devolución: {ex.Message}");
            }
        }
    }
}
