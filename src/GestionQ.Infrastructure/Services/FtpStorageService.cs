using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GestionQ.Infrastructure.Services
{
    public class FtpStorageService : ICloudStorageService
    {
        private readonly IServiceProvider _serviceProvider;

        public FtpStorageService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<string> UploadInvoicePdfAsync(byte[] pdfBytes, string fileName)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GestionQ.Infrastructure.Data.ApplicationDbContext>();
            var settings = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToDictionaryAsync(db.SystemSettings, x => x.Key, x => x.Value);

            bool isEnabled = settings.GetValueOrDefault("Cloud_FtpEnabled") == "true";
            if (!isEnabled)
            {
                return null;
            }

            string ftpHost = settings.GetValueOrDefault("Cloud_FtpHost"); // e.g., ftp://ftp.midominio.com
            string ftpUser = settings.GetValueOrDefault("Cloud_FtpUser");
            string ftpPassword = settings.GetValueOrDefault("Cloud_FtpPassword");
            string remoteFolder = settings.GetValueOrDefault("Cloud_FtpRemoteFolder", "/"); // e.g., /public_html/facturas/
            string publicDomain = settings.GetValueOrDefault("Cloud_PublicDomain"); // e.g., https://midominio.com/facturas/

            if (string.IsNullOrEmpty(ftpHost) || string.IsNullOrEmpty(publicDomain))
            {
                return null;
            }

            if (!ftpHost.StartsWith("ftp://"))
            {
                ftpHost = "ftp://" + ftpHost;
            }
            if (!ftpHost.EndsWith("/"))
            {
                ftpHost += "/";
            }
            
            // Cleanup remote folder slashes
            string safeRemoteFolder = remoteFolder.TrimStart('/').TrimEnd('/');
            if (!string.IsNullOrEmpty(safeRemoteFolder))
            {
                safeRemoteFolder += "/";
            }

            string requestUri = $"{ftpHost}{safeRemoteFolder}{fileName}";
            
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(requestUri);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            
            if (!string.IsNullOrEmpty(ftpUser))
            {
                request.Credentials = new NetworkCredential(ftpUser, ftpPassword);
            }
            
            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = false;

            using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != FtpStatusCode.ClosingData)
                {
                    throw new Exception($"FTP Upload failed. Status: {response.StatusDescription}");
                }
            }

            // Return the public URL
            if (!publicDomain.EndsWith("/"))
            {
                publicDomain += "/";
            }

            return $"{publicDomain}{fileName}";
        }
    }
}
