using Npgsql;

namespace eMilimo.API.Platform;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken cancellationToken = default);
}

public sealed class PostgreSqlDistributedLock(IConfiguration configuration) : IDistributedLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(string resource, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Default database connection is not configured.");
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("select pg_try_advisory_lock(hashtext(@resource))", connection);
        command.Parameters.AddWithValue("resource", resource);
        if (await command.ExecuteScalarAsync(cancellationToken) is not true)
        {
            await connection.DisposeAsync();
            return null;
        }
        return new AdvisoryLock(connection, resource);
    }

    private sealed class AdvisoryLock(NpgsqlConnection connection, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("select pg_advisory_unlock(hashtext(@resource))", connection);
                command.Parameters.AddWithValue("resource", resource);
                await command.ExecuteNonQueryAsync();
            }
            finally { await connection.DisposeAsync(); }
        }
    }
}
