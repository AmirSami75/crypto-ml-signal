namespace CryptoMlSignal.Api.Options;

public sealed class MlServiceOptions
{
    public const string SectionName = "MlService";

    public string BaseUrl { get; init; } = "http://localhost:8001";

    public int TimeoutSeconds { get; init; } = 10;
}
