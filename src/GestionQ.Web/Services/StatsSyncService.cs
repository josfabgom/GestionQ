using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionQ.Web.Services
{
    public class StatsSyncService : BackgroundService
    {
        private readonly ILogger<StatsSyncService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _vpsSyncUrl;

        public StatsSyncService(ILogger<StatsSyncService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            
            _apiKey = _configuration["ApiKey"];
            var baseUrl = _configuration["VpsSyncUrl"]?.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl)) {
                _vpsSyncUrl = "https://galopsrl.com.ar/api/sync";
            } else {
                _vpsSyncUrl = baseUrl.EndsWith("/api/sync") ? baseUrl : baseUrl + "/api/sync";
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stats Sync Service is starting.");

            // Wait a bit before first sync to let the API start fully
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SyncStatsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while syncing stats to VPS.");
                }

                // Esperar 1 minuto entre sincronizaciones (para demo)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task SyncStatsAsync()
        {
            _logger.LogInformation("Recopilando estadísticas locales...");
            var client = _httpClientFactory.CreateClient();
            
            // Loopback request to our own API to get the summary
            var localRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/summary");
            localRequest.Headers.Add("X-Api-Key", _apiKey);
            var localResponse = await client.SendAsync(localRequest);
            localResponse.EnsureSuccessStatusCode();
            var summaryData = await localResponse.Content.ReadFromJsonAsync<object>();

            // Get POS Status
            var posRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/pos-status");
            posRequest.Headers.Add("X-Api-Key", _apiKey);
            var posResponse = await client.SendAsync(posRequest);
            object? posData = null;
            if (posResponse.IsSuccessStatusCode)
            {
                posData = await posResponse.Content.ReadFromJsonAsync<object>();
            }

            // Get POS History
            var posHistRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/pos-history?days=30");
            posHistRequest.Headers.Add("X-Api-Key", _apiKey);
            var posHistResponse = await client.SendAsync(posHistRequest);
            object? posHistData = null;
            if (posHistResponse.IsSuccessStatusCode)
            {
                posHistData = await posHistResponse.Content.ReadFromJsonAsync<object>();
            }

            // Get Top Products
            var topProdRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/top-products?limit=20");
            topProdRequest.Headers.Add("X-Api-Key", _apiKey);
            var topProdResponse = await client.SendAsync(topProdRequest);
            object? topProductsData = null;
            if (topProdResponse.IsSuccessStatusCode)
            {
                topProductsData = await topProdResponse.Content.ReadFromJsonAsync<object>();
            }

            // Get Sales By Payment
                        var salesByPaymentRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/sales-by-payment?days=7");
            salesByPaymentRequest.Headers.Add("X-Api-Key", _apiKey);
            var salesByPaymentResponse = await client.SendAsync(salesByPaymentRequest);
            object? salesByPaymentData = null;
            if (salesByPaymentResponse.IsSuccessStatusCode)
            {
                salesByPaymentData = await salesByPaymentResponse.Content.ReadFromJsonAsync<object>();
            }

            // Get Raw Sales
            var rawSalesRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5144/api/stats/raw-sales?days=90");
            rawSalesRequest.Headers.Add("X-Api-Key", _apiKey);
            var rawSalesResponse = await client.SendAsync(rawSalesRequest);
            object? rawSalesData = null;
            if (rawSalesResponse.IsSuccessStatusCode)
            {
                rawSalesData = await rawSalesResponse.Content.ReadFromJsonAsync<object>();
            }

            // Empaquetar para enviar a la VPS
            var payload = new
            {
                summary = summaryData,
                posBoxes = posData,
                posHistory = posHistData,
                topProducts = topProductsData,
                salesByPayment = salesByPaymentData,
                rawSales = rawSalesData
            };

            _logger.LogInformation($"Enviando estadísticas a la VPS ({_vpsSyncUrl})...");
            
            var vpsRequest = new HttpRequestMessage(HttpMethod.Post, _vpsSyncUrl);
            vpsRequest.Headers.Add("X-Api-Key", _apiKey);
            vpsRequest.Content = JsonContent.Create(payload);

            var vpsResponse = await client.SendAsync(vpsRequest);
            
            if (vpsResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("¡Estadísticas sincronizadas exitosamente con la VPS!");
            }
            else
            {
                var errorBody = await vpsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"VPS rechazó la sincronización: {vpsResponse.StatusCode} - {errorBody}");
            }
        }
    }
}


