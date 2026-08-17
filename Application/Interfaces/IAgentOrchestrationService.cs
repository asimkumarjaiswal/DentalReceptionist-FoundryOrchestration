namespace VoiceDentalReceptionist.Application.Interfaces;

/// <summary>
/// Application-layer abstraction over Foundry invocation (spec section 11) -
/// no Foundry SDK types appear in this signature.
/// </summary>
public interface IAgentOrchestrationService
{
    Task<AgentResponse> SendMessageAsync(
        string conversationId,
        string message,
        CancellationToken cancellationToken);
}

public record AgentResponse(string ConversationId, string Message, string Agent);
