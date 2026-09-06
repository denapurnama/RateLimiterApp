using StackExchange.Redis;

namespace RateLimiterApp.Services;

public class RedisRateLimiterService : IRateLimiterService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly string SlidingWindowLuaScript = @"
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local limit = tonumber(ARGV[3])
        local clearBefore = now - window

        -- 1. Hapus request lama di luar window waktu
        redis.call('ZREMRANGEBYSCORE', key, '-inf', clearBefore)

        -- 2. Hitung jumlah request saat ini dalam window
        local currentRequests = redis.call('ZCARD', key)

        -- 3. Cek batas limit
        if currentRequests < limit then
            redis.call('ZADD', key, now, now)
            redis.call('EXPIRE', key, window)
            return { 1, limit - currentRequests - 1 }
        else
            return { 0, 0 }
        end
    ";

    public RedisRateLimiterService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<(bool IsAllowed, int RemainingRequests)> CheckRateLimitAsync(string clientKey, int limit, TimeSpan window)
    {
        var db = _redis.GetDatabase();
        var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowSeconds = (long)window.TotalSeconds;

        var redisKey = $"rate_limit:{clientKey}";
        var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
            SlidingWindowLuaScript,
            keys: new RedisKey[] { redisKey },
            values: new RedisValue[] { nowUnixSeconds, windowSeconds, limit }
        );

        if (result != null && result.Length == 2)
        {
            var isAllowed = (long)result[0] == 1;
            var remaining = (int)(long)result[1];
            return (isAllowed, remaining);
        }

        return (false, 0);
    }
}