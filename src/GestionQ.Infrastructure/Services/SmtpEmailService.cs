using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using GestionQ.Infrastructure.Data;

namespace GestionQ.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;

        public SmtpEmailService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string? pdfAttachmentName = null, byte[]? pdfAttachmentBytes = null)
        {
            var settings = await _context.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);

            string host = settings.GetValueOrDefault("SmtpHost", "");
            string portStr = settings.GetValueOrDefault("SmtpPort", "587");
            string email = settings.GetValueOrDefault("SmtpEmail", "");
            string password = settings.GetValueOrDefault("SmtpPassword", "");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("La configuración SMTP no está completa. Por favor, configure el servidor de correo en el panel de Configuración.");
            }

            int port = int.TryParse(portStr, out int p) ? p : 587;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("GestiónQ", email));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };

            if (pdfAttachmentBytes != null && !string.IsNullOrEmpty(pdfAttachmentName))
            {
                builder.Attachments.Add(pdfAttachmentName, pdfAttachmentBytes, ContentType.Parse("application/pdf"));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(email, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al enviar el correo: {ex.Message}", ex);
            }
        }
    }
}
