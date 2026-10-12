using System.Threading;
using System.Threading.Tasks;
using TexasPortal.Application.Features.Documents.Models;

namespace TexasPortal.Application.Features.Documents.Services;

public interface IDocumentPdfService
{
    Task<byte[]> GeneratePdfAsync(DocumentDataModel model, CancellationToken ct = default);
}
