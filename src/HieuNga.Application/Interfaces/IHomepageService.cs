using HieuNga.Application.DTOs;

namespace HieuNga.Application.Interfaces;

public interface IHomepageService
{
    Task<HomepageDto> GetHomepageDataAsync(CancellationToken ct = default);
}
