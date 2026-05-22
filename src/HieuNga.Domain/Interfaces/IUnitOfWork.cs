namespace HieuNga.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
