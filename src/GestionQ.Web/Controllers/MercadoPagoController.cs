using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionQ.Infrastructure.Data;
using GestionQ.Domain.Entities;
using GestionQ.Infrastructure.Services;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace GestionQ.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MercadoPagoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMercadoPagoService _mpService;
        private readonly IConfiguration _configuration;

        public MercadoPagoController(ApplicationDbContext context, IMercadoPagoService mpService, IConfiguration configuration)
        {
            _context = context;
            _mpService = mpService;
            _configuration = configuration;
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] MpOrderRequest request)
        {
            // Get active config, prioritizing specific PointOfSale configuration
            var config = await _context.MercadoPagoConfigs
                .Where(c => c.IsActive && (c.PointOfSaleId == null || c.PointOfSaleId == request.PointOfSaleId))
                .OrderByDescending(c => c.PointOfSaleId.HasValue)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                return BadRequest(new { success = false, message = "No hay una configuración de Mercado Pago activa para esta caja configurada en el Panel del Sistema." });
            }

            if (!string.IsNullOrEmpty(config.RefreshToken) && (!config.ExpiresAt.HasValue || config.ExpiresAt.Value < DateTime.UtcNow.AddDays(15)))
            {
                var refreshed = await RefreshTokenAsync(config);
                if (!refreshed)
                {
                    return BadRequest(new { success = false, message = "La sesión de Mercado Pago expiró y no pudo ser renovada. Vuelva a vincular su cuenta desde Configuración." });
                }
            }

            var reference = $"VTA-{DateTime.Now.Ticks}";
            MpOrderResponse mpResponse;

            var method = request.Method;
            if (string.IsNullOrEmpty(method))
            {
                method = config.DefaultMethod;
            }

            if (method == "QR")
            {
                mpResponse = await _mpService.CreateQrOrderAsync(config, reference, request.Amount, "Venta en Caja");
            }
            else
            {
                mpResponse = await _mpService.CreatePointOrderAsync(config, reference, request.Amount, "Venta en Caja");
            }

            if (mpResponse.Success)
            {
                return Ok(new
                {
                    success = true,
                    method = method,
                    reference = reference,
                    qrData = mpResponse.QrData,
                    orderId = mpResponse.OrderId
                });
            }

            return BadRequest(new { success = false, message = mpResponse.ErrorMessage });
        }

        [HttpGet("check-status/{reference}")]
        public async Task<IActionResult> CheckStatus(string reference, [FromQuery] int? pointOfSaleId)
        {
            var config = await _context.MercadoPagoConfigs
                .Where(c => c.IsActive && (c.PointOfSaleId == null || c.PointOfSaleId == pointOfSaleId))
                .OrderByDescending(c => c.PointOfSaleId.HasValue)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                return BadRequest("No config en el Panel");
            }

            var result = await _mpService.CheckOrderStatusAsync(config, reference);
            return Ok(new { isPaid = result.IsPaid, status = result.Status });
        }

        [HttpGet("patch-terminal")]
        [AllowAnonymous]
        public async Task<IActionResult> ForcePDV()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config en el Panel");

            var requestUrl = "https://api.mercadopago.com/point/integration-api/devices/PAX_A910__SMARTPOS1495446935";
            using var request = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod("PATCH"), requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
            request.Content = new System.Net.Http.StringContent("{\"operating_mode\": \"PDV\"}", System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Exito = response.IsSuccessStatusCode, 
                CodigoHttp = response.StatusCode, 
                RespuestaMP = content
            });
        }

        [HttpGet("get-device")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDevice()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config");

            var deviceId = "PAX_A910__SMARTPOS1495446935";
            var requestUrl = $"https://api.mercadopago.com/point/integration-api/devices/{deviceId}";
                
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Status = response.StatusCode,
                Body = content
            });
        }

        [HttpGet("get-pos-list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPosList()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config");

            var userId = await GetUserIdFromTokenAsync(config.AccessToken);
            var requestUrl = $"https://api.mercadopago.com/pos";
                
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Status = response.StatusCode,
                Body = content
            });
        }

        private async Task<string> GetUserIdFromTokenAsync(string accessToken)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://api.mercadopago.com/users/me");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(contentString);
                    return doc.RootElement.GetProperty("id").GetInt64().ToString();
                }
            }
            catch { }
            return null;
        }

        [HttpGet("oauth-url")]
        public IActionResult GetOAuthUrl()
        {
            var clientId = _configuration["MercadoPagoApp:ClientId"];
            var redirectUri = _configuration["MercadoPagoApp:RedirectUri"];
            var url = $"https://auth.mercadopago.com/authorization?client_id={clientId}&response_type=code&platform_id=mp&redirect_uri={redirectUri}";
            return Ok(new { url });
        }

        [HttpGet("oauth-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string state = null)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Missing code");

            var clientId = _configuration["MercadoPagoApp:ClientId"];
            var clientSecret = _configuration["MercadoPagoApp:ClientSecret"];
            var redirectUri = _configuration["MercadoPagoApp:RedirectUri"];

            var content = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "authorization_code"),
                new System.Collections.Generic.KeyValuePair<string, string>("code", code),
                new System.Collections.Generic.KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.PostAsync("https://api.mercadopago.com/oauth/token", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();
                var refreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
                var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
                var userId = doc.RootElement.GetProperty("user_id").GetInt64();

                var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive && c.MpUserId == userId).FirstOrDefaultAsync();
                if (config == null)
                {
                    config = new MercadoPagoConfig { IsActive = true, DefaultMethod = "QR" };
                    _context.MercadoPagoConfigs.Add(config);
                }

                config.AccessToken = accessToken;
                config.RefreshToken = refreshToken;
                config.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                config.MpUserId = userId;

                await _context.SaveChangesAsync();

                return Redirect("/MercadoPagoConfig?success=true");
            }

            return BadRequest("Failed to exchange code for token: " + await response.Content.ReadAsStringAsync());
        }


        [HttpGet("update-pos")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdatePos()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config");

            var requestUrl = "https://api.mercadopago.com/pos/106644730";
                
            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Put, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
            request.Content = new System.Net.Http.StringContent("{\"name\": \"QR_AU\", \"external_id\": \"QRAU\", \"fixed_amount\": false}", System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Status = response.StatusCode,
                Body = content
            });
        }

        [HttpGet("test-charge")]
        [AllowAnonymous]
        public async Task<IActionResult> TestCharge()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config");

            var deviceId = "PAX_A910__SMARTPOS1495446935";
            var requestUrl = $"https://api.mercadopago.com/point/integration-api/devices/{deviceId}/payment-intents";
                
            var payload = new
            {
                amount = 1500,
                additional_info = new
                {
                    external_reference = "TEST-12345",
                    print_on_terminal = true
                }
            };

            using var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
            request.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Status = response.StatusCode,
                Body = content
            });
        }

        [HttpGet("force-pdv")]
        [AllowAnonymous]
        public async Task<IActionResult> ForcePdv()
        {
            var config = await _context.MercadoPagoConfigs.Where(c => c.IsActive).OrderByDescending(c => c.Id).FirstOrDefaultAsync();
            if (config == null) return BadRequest("No config");

            var deviceId = "PAX_A910__SMARTPOS1495446935";
            var requestUrl = $"https://api.mercadopago.com/point/integration-api/devices/{deviceId}";
                
            using var request = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod("PATCH"), requestUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.AccessToken);
            request.Content = new System.Net.Http.StringContent("{\"operating_mode\": \"PDV\"}", System.Text.Encoding.UTF8, "application/json");

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return Ok(new { 
                Status = response.StatusCode,
                Body = content
            });
        }
        private async Task<bool> RefreshTokenAsync(MercadoPagoConfig config)
        {
            var clientId = _configuration["MercadoPagoApp:ClientId"];
            var clientSecret = _configuration["MercadoPagoApp:ClientSecret"];

            var content = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "refresh_token"),
                new System.Collections.Generic.KeyValuePair<string, string>("refresh_token", config.RefreshToken)
            });

            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.PostAsync("https://api.mercadopago.com/oauth/token", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                
                config.AccessToken = doc.RootElement.GetProperty("access_token").GetString();
                config.RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
                config.ExpiresAt = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());
                
                _context.MercadoPagoConfigs.Update(config);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }

    public class MpOrderRequest
    {
        public decimal Amount { get; set; }
        public int? PointOfSaleId { get; set; }
        public string? Method { get; set; }
    }
}
