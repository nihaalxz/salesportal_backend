using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Features.Invoices.Models;

namespace TexasPortal.Application.Features.Invoices.Services;

public interface IInvoicePdfService
{
    Task<byte[]> GeneratePdfAsync(InvoiceDataModel model, CancellationToken ct = default);
}
