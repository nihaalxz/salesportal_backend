using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Domain.Common;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateDocumentNumberAsync(string prefix, string sequenceName, CancellationToken ct = default);
}
