using RateLimiterApp.Middleware;
using RateLimiterApp.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Register Redis Connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// Register Rate Limiter Service
builder.Services.AddSingleton<IRateLimiterService, RedisRateLimiterService>();

var app = builder.Build();

// Enable Rate Limiting Middleware
app.UseMiddleware<RateLimitingMiddleware>();

// Sample API Endpoint
app.MapGet("/api/data", () => Results.Ok(new
{
    Message = "Berhasil mengakses data terlindungi!",
    Timestamp = DateTime.UtcNow
}));

app.Run();