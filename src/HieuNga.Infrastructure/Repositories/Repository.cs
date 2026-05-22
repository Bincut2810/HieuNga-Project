using System.Linq.Expressions;
using HieuNga.Domain.Common;
using HieuNga.Domain.Interfaces;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.Repositories;

public class Repository<T>(HieuNgaDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) =>
        await DbSet.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(T entity, CancellationToken ct = default)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        return UpdateAsync(entity, ct);
    }
}
