using GestionQ.Domain.Entities;
using System.Threading.Tasks;

namespace GestionQ.Infrastructure.Services
{
    public interface IInvoicePdfGenerator
    {
        byte[] GeneratePdf(ElectronicInvoice invoice, string companyName, string companyCuit, string companyCondition, string companyAddress);
    }
}
