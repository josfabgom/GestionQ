using System.Threading.Tasks;

namespace GestionQ.Infrastructure.Services
{
    public interface ICloudStorageService
    {
        Task<string> UploadInvoicePdfAsync(byte[] pdfBytes, string fileName);
    }
}
