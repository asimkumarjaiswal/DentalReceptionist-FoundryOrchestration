namespace VoiceDentalReceptionist.Agents.Foundry;

/// <summary>
/// Internal result of invoking the Foundry agent for one turn. Not exposed
/// over HTTP directly - AgentOrchestrationService maps this to the public
/// SendMessageResponse DTO (spec section 11: "Do not expose Foundry SDK
/// types from application interfaces").
/// </summary>
public record AgentInvocationResult
{
    public required string ResponseText { get; init; }
    public required string LastResponseId { get; init; }
    public IReadOnlyList<string> ToolsInvoked { get; init; } = Array.Empty<string>();
}
