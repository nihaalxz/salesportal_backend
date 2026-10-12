using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Features.Invoices.Models;

namespace TexasPortal.Application.Features.Invoices.Builders;

public interface IInvoiceBuilder
{
    string SupportedEntityType { get; }
    Task<InvoiceDataModel?> BuildAsync(long id, long userId, CancellationToken ct = default);
}
