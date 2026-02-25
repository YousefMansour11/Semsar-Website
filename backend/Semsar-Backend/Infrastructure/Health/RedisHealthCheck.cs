using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Infrastructure.Health;

public class RedisHealthCheck : IAppHealthCheck
{
    private static ConnectionMultiplexer? _connection;
    private static readonly object _lock = new();
    private readonly string _connectionString;

    public RedisHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string Name => "Redis";

    public async Task<(bool Healthy, string? Description)> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection == null || !_connection.IsConnected)
            {
                lock (_lock)
                {
                    if (_connection == null || !_connection.IsConnected)
                    {
                        _connection?.Dispose();
                        _connection = ConnectionMultiplexer.Connect(_connectionString);
                    }
                }
            }
            return (true, "Connected");
        }
        catch (System.Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
