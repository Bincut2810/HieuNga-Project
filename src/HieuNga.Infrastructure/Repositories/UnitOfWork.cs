using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;

namespace HieuNga.Infrastructure.Repositories;

public class UnitOfWork(HieuNgaDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
