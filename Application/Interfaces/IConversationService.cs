namespace VoiceDentalReceptionist.Application.Interfaces;

public interface IConversationService
{
    string CreateConversation();

    Task<AgentResponse> SendMessageAsync(
        string conversationId,
        string message,
        CancellationToken cancellationToken);
}
