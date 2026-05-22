using HieuNga.Application.DTOs;
using HieuNga.Application.Interfaces;
using HieuNga.Application.Mappings;
using HieuNga.Domain.Interfaces;

namespace HieuNga.Application.Services;

public class BranchService(IBranchRepository repository) : IBranchService
{
    public async Task<IReadOnlyList<BranchDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var branches = await repository.GetActiveAsync(ct);
        return branches.Select(b => b.ToDto()).ToList();
    }
}
