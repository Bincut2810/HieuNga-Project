using HieuNga.Domain.Entities;

namespace HieuNga.Domain.Interfaces;

public interface IBranchRepository : IRepository<Branch>
{
    Task<IReadOnlyList<Branch>> GetActiveAsync(CancellationToken ct = default);
}
