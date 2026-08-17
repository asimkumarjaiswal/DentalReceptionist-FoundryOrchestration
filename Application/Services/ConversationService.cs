using VoiceDentalReceptionist.Application.Interfaces;

namespace VoiceDentalReceptionist.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationStore _conversationStore;
    private readonly IAgentOrchestrationService _orchestrationService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationStore conversationStore,
        IAgentOrchestrationService orchestrationService,
        ILogger<ConversationService> logger)
    {
        _conversationStore = conversationStore;
        _orchestrationService = orchestrationService;
        _logger = logger;
    }

    public string CreateConversation()
    {
        var conversationId = _conversationStore.CreateConversation();
        _logger.LogInformation("Conversation created. ConversationId={ConversationId}", conversationId);
        return conversationId;
    }

    public async Task<AgentResponse> SendMessageAsync(
        string conversationId,
        string message,
        CancellationToken cancellationToken)
    {
        if (!_conversationStore.Exists(conversationId))
            throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");

        _logger.LogInformation("Message received. ConversationId={ConversationId}", conversationId);

        var response = await _orchestrationService.SendMessageAsync(conversationId, message, cancellationToken);

        _logger.LogInformation("Request completed. ConversationId={ConversationId}", conversationId);
        return response;
    }
}
