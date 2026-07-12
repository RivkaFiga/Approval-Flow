namespace ApprovalFlow.AiDecision.Infrastructure.Agents;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Gemini API key. Set via environment variable GEMINI__APIKEY in non-local environments.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-1.5-flash";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When true the <see cref="StubPolicyAgent"/> is used instead of Gemini.
    /// Defaults to true so local dev works without an API key.
    /// </summary>
    public bool UseStub { get; set; } = true;
}
