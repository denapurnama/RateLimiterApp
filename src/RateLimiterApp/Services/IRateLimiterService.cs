namespace RateLimiterApp.Services;

public interface IRateLimiterService
{
    Task<(bool IsAllowed, int RemainingRequests)> CheckRateLimitAsync(string clientKey, int limit, TimeSpan window);
}