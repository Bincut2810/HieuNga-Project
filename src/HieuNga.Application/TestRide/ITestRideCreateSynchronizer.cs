namespace HieuNga.Application.TestRide;

/// <summary>
/// Serializes create attempts that share the same idempotency key (phone + motorcycle + day).
/// </summary>
public interface ITestRideCreateSynchronizer
{
    Task<T> ExecuteAsync<T>(
        string lockKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
