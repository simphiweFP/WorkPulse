namespace WorkPulse.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}