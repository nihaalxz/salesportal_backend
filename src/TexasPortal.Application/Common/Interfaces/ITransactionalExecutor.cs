using System;
using System.Threading;
using System.Threading.Tasks;

namespace TexasPortal.Application.Common.Interfaces;

public interface ITransactionalExecutor
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
