using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using NotasNet.Api.Application;
using NotasNet.Api.Endpoints;
using NotasNet.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

if (!string.IsNullOrWhiteSpace(config["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitor();

var allowedOrigin = config["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigin).WithMethods("POST").WithHeaders("Content-Type")));

var perMinute = config.GetValue("RateLimit:PerMinute", 5);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("notes", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = perMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddSingleton(new GeminiOptions(
    config["Gemini:ApiKey"] ?? "",
    config["Gemini:Model"] ?? "gemini-2.5-flash"));
builder.Services.AddHttpClient<ILlmProvider, GeminiLlmProvider>();
builder.Services.AddSingleton<ITranscriptProvider, YoutubeTranscriptProvider>();
builder.Services.AddSingleton<IUsageQuota>(new DailyQuotaLimiter(config.GetValue("Notes:DailyLimit", 200)));
builder.Services.AddSingleton(new NotesOptions(
    config.GetValue("Notes:MaxTranscriptChars", 400_000),
    config.GetValue("Notes:ChunkChars", 60_000),
    config.GetValue("Notes:MinWords", 30),
    PromptLoader.Load("notes.system.md")));
builder.Services.AddScoped<GenerateNotesUseCase>();

// Behind Azure Container Apps ingress the client IP only arrives in X-Forwarded-For;
// without this every visitor would share one rate-limit bucket.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors();
app.UseRateLimiter();
app.MapGet("/health", () => "ok");
app.MapNotesEndpoint();

app.Run();

public partial class Program;
