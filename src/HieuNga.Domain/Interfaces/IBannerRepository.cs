using HieuNga.Domain.Entities;

namespace HieuNga.Domain.Interfaces;

public interface IBannerRepository : IRepository<Banner>
{
    Task<IReadOnlyList<Banner>> GetHomepageBannersAsync(int max = 5, CancellationToken ct = default);
}
