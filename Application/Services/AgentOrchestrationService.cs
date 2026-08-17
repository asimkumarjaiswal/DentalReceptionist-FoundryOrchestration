using VoiceDentalReceptionist.Agents.Foundry;
using VoiceDentalReceptionist.Application.Interfaces;

namespace VoiceDentalReceptionist.Application.Services;

public class AgentOrchestrationService : IAgentOrchestrationService
{
    private readonly FoundryAgentService _foundryAgentService;
    private readonly IConversationStore _conversationStore;
    private readonly AppConfig _config;
    private readonly ILogger<AgentOrchestrationService> _logger;

    public AgentOrchestrationService(
        FoundryAgentService foundryAgentService,
        IConversationStore conversationStore,
        AppConfig config,
        ILogger<AgentOrchestrationService> logger)
    {
        _foundryAgentService = foundryAgentService;
        _conversationStore = conversationStore;
        _config = config;
        _logger = logger;
    }

    public async Task<AgentResponse> SendMessageAsync(string conversationId, string message, CancellationToken cancellationToken)
    {
        var previousResponseId = _conversationStore.GetLastResponseId(conversationId);

        AgentInvocationResult result = await _foundryAgentService.SendMessageAsync(previousResponseId, message, conversationId, cancellationToken);

        _conversationStore.SetLastResponseId(conversationId, result.LastResponseId);

        if (result.ToolsInvoked.Count > 0)
        {
            _logger.LogInformation("[FOUNDRY] Delegation/tools this turn: {Tools}. ConversationId={ConversationId}",
                string.Join(",", result.ToolsInvoked), conversationId);
        }

        return new AgentResponse(conversationId, result.ResponseText, _config.FoundryAgentName);
    }
}
