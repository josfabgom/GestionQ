using GestionQ.Domain.Entities;
using System.Threading.Tasks;

namespace GestionQ.Infrastructure.Services
{
    public class MpOrderResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string QrData { get; set; }
        public string OrderId { get; set; }
    }

    public class MpPaymentStatusResponse
    {
        public bool IsPaid { get; set; }
        public string Status { get; set; }
    }

    public interface IMercadoPagoService
    {
        Task<MpOrderResponse> CreateQrOrderAsync(MercadoPagoConfig config, string reference, decimal amount, string description);
        Task<MpOrderResponse> CreatePointOrderAsync(MercadoPagoConfig config, string reference, decimal amount, string description);
        Task<MpPaymentStatusResponse> CheckOrderStatusAsync(MercadoPagoConfig config, string reference);
        Task<(bool Success, string ErrorMessage)> RefundPaymentAsync(string paymentId, string accessToken);
    }
}
