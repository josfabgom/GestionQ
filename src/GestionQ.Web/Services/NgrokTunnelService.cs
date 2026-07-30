using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionQ.Web.Services
{
    public class NgrokTunnelService : IHostedService, IDisposable
    {
        private readonly ILogger<NgrokTunnelService> _logger;
        private readonly IConfiguration _configuration;
        private Process _process;
        
        public NgrokTunnelService(ILogger<NgrokTunnelService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando Ngrok Tunnel...");

            try
            {
                var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ngrok.exe");
                if (!File.Exists(exePath))
                {
                    _logger.LogWarning($"No se encontro ngrok.exe en {exePath}. El tunel no se iniciara.");
                    return Task.CompletedTask;
                }

                var authToken = _configuration["Ngrok:AuthToken"];
                var domain = _configuration["Ngrok:Domain"];

                if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(domain))
                {
                    _logger.LogWarning("Falta AuthToken o Domain de Ngrok en appsettings.json. El tunel no se iniciara.");
                    return Task.CompletedTask;
                }

                // Configurar Authtoken
                var authProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"config add-authtoken {authToken}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                authProcess?.WaitForExit();

                // Iniciar tunel
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = $"http --domain={domain} 5144",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _process.Start();
                _logger.LogInformation($"[Ngrok] Tunel iniciado en https://{domain}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar el tunel de Ngrok");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(true);
                    _process.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al detener el tunel de Ngrok");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try { _process?.Dispose(); } catch { }
        }
    }
}
