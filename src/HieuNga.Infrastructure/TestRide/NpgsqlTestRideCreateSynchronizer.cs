using System.Security.Cryptography;
using System.Text;
using HieuNga.Application.TestRide;
using HieuNga.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HieuNga.Infrastructure.TestRide;

/// <summary>
/// PostgreSQL transaction + advisory lock — prevents concurrent duplicate TestRide creates
/// without schema changes.
/// </summary>
public sealed class NpgsqlTestRideCreateSynchronizer(HieuNgaDbContext db) : ITestRideCreateSynchronizer
{
    public async Task<T> ExecuteAsync<T>(
        string lockKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lockKey);
        ArgumentNullException.ThrowIfNull(operation);

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var key = StableInt64(lockKey);
            await db.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock({key})",
                cancellationToken);

            var result = await operation(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static long StableInt64(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToInt64(hash, 0);
    }
}
