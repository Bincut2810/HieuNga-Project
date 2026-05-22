using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;

namespace HieuNga.Domain.Interfaces;

public interface IBannerRepository : IRepository<Banner>
{
    Task<IReadOnlyList<Banner>> GetByPositionAsync(BannerPosition position, CancellationToken ct = default);
}
