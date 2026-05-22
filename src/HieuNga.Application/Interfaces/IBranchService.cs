using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> GetActiveAsync(CancellationToken ct = default);
}
