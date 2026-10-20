namespace CryptoMlSignal.Api.Contracts;

public sealed record MlServiceHealth(
    string Service,
    string Status,
    string OperatingMode,
    DateTimeOffset Timestamp);

public sealed record MlServiceCapabilities(
    string Service,
    string[] Capabilities,
    string[] SupportedJobTypes);

public sealed record DependencyHealth(
    string Name,
    string Status,
    string? Detail = null);
