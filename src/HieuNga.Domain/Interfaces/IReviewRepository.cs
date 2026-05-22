using HieuNga.Domain.Entities;

namespace HieuNga.Domain.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetFeaturedAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByMotorcycleAsync(Guid motorcycleId, CancellationToken ct = default);
}
