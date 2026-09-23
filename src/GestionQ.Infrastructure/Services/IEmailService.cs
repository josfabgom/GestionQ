using System.Threading.Tasks;

namespace GestionQ.Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string? pdfAttachmentName = null, byte[]? pdfAttachmentBytes = null);
    }
}
