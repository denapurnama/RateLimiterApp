using System.Net;
using RateLimiterApp.Services;

namespace RateLimiterApp.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private const int MaxRequests = 5;                  // Batas Max 5 Request
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10); // Dalam Window 10 Detik

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimiterService rateLimiter)
    {
        // Ambil IP Client (atau dari Header 'X-API-KEY' jika ada)
        var clientId = context.Request.Headers["X-API-KEY"].FirstOrDefault()
                       ?? context.Connection.RemoteIpAddress?.ToString()
                       ?? "anonymous";

        var (isAllowed, remainingRequests) = await rateLimiter.CheckRateLimitAsync(clientId, MaxRequests, Window);

        // Set Standard Rate Limit Response Headers
        context.Response.Headers["X-RateLimit-Limit"] = MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, remainingRequests).ToString();

        if (!isAllowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = Window.TotalSeconds.ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                Title = "Too Many Requests",
                Status = 429,
                Detail = $"Kuota request Anda telah habis. Batas maksimum adalah {MaxRequests} request per {Window.TotalSeconds} detik."
            });
            return;
        }

        await _next(context);
    }
}