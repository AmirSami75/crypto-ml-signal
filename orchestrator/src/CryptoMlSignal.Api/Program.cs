using CryptoMlSignal.Api.Clients;
using CryptoMlSignal.Api.Options;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MlServiceOptions>(
    builder.Configuration.GetSection(MlServiceOptions.SectionName));
builder.Services.AddHttpClient<IMlServiceClient, MlServiceClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<MlServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(item => item.Value)
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .Cast<string>()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/health/live", () => Results.Ok(new
{
    service = "dotnet-orchestrator",
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow,
}));

app.MapGet("/health/ready", async (
    IMlServiceClient mlService,
    CancellationToken cancellationToken) =>
{
    var mlHealth = await mlService.GetHealthAsync(cancellationToken);
    var isReady = mlHealth.Status.Equals("healthy", StringComparison.OrdinalIgnoreCase);
    return Results.Json(
        new
        {
            service = "dotnet-orchestrator",
            status = isReady ? "healthy" : "degraded",
            dependencies = new[] { mlHealth },
            timestamp = DateTimeOffset.UtcNow,
        },
        statusCode: isReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
});

var api = app.MapGroup("/api/v1");

api.MapGet("/", () => Results.Ok(new
{
    name = "Crypto ML Trading Platform API",
    version = "v1",
    orchestrator = "dotnet-10",
    links = new
    {
        platform = "/api/v1/platform",
        mlCapabilities = "/api/v1/ml/capabilities",
        liveHealth = "/health/live",
        readyHealth = "/health/ready",
    },
}));

api.MapGet("/platform", (IConfiguration configuration) => Results.Ok(new
{
    backend = ".NET 10 ASP.NET Core API and orchestrator",
    machineLearning = "Python internal ML service",
    dashboard = "React and TypeScript",
    database = "PostgreSQL",
    operatingMode = configuration["OperatingMode"] ?? "PAPER",
    executionPolicy = "Only the .NET orchestrator may authorize exchange orders",
}));

api.MapGet("/ml/capabilities", async (
    IMlServiceClient mlService,
    CancellationToken cancellationToken) =>
{
    var capabilities = await mlService.GetCapabilitiesAsync(cancellationToken);
    return capabilities is null
        ? Results.Problem(
            title: "Python ML service unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(capabilities);
});

app.Run();
