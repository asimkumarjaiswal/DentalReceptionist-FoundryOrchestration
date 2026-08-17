using System.ClientModel;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;
using VoiceDentalReceptionist.Tools;

namespace VoiceDentalReceptionist.Agents.Foundry;

public class FoundryAgentService
{
    private readonly ProjectResponsesClient _responseClient;
    private readonly AppointmentTools _appointmentTools;
    private readonly CallbackTools _callbackTools;
    private readonly ILogger<FoundryAgentService> _logger;
    private readonly string _agentName;

    public FoundryAgentService(
        AppConfig config,
        AppointmentTools appointmentTools,
        CallbackTools callbackTools,
        ILogger<FoundryAgentService> logger)
    {
        _appointmentTools = appointmentTools;
        _callbackTools = callbackTools;
        _logger = logger;
        _agentName = config.FoundryAgentName;

        var projectClient = new AIProjectClient(
            new Uri(config.FoundryProjectEndpoint),
            new AzureCliCredential());

        _responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(_agentName);
    }

    public async Task<AgentInvocationResult> SendMessageAsync(
        string? previousResponseId,
        string message,
        string conversationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[FOUNDRY] Invocation started. ConversationId={ConversationId} Agent={Agent}",
            conversationId, _agentName);

        var toolsInvoked = new List<string>();
        #pragma warning disable OPENAI001
        var inputItems = new List<ResponseItem> { ResponseItem.CreateUserMessageItem(message) };
        ResponseResult response;
        bool moreToolCalls;

        try 
        {
            do
            {
                var options = new CreateResponseOptions { PreviousResponseId = previousResponseId };
                foreach (var item in inputItems) options.InputItems.Add(item);

                ClientResult<ResponseResult> clientResult =
                    await _responseClient.CreateResponseAsync(options, cancellationToken);
                response = clientResult.Value;
                previousResponseId = response.Id;

                inputItems = new List<ResponseItem>();
                moreToolCalls = false;

                foreach (ResponseItem outputItem in response.OutputItems)
                {
                    if (outputItem is FunctionCallResponseItem functionCall)
                    {
                        _logger.LogInformation(
                            "[FOUNDRY] Tool invoked: {ToolName}. ConversationId={ConversationId}",
                            functionCall.FunctionName, conversationId);
                        toolsInvoked.Add(functionCall.FunctionName);

                        string toolResult = await ExecuteToolAsync(
                            functionCall.FunctionName,
                            functionCall.FunctionArguments.ToString());

                        _logger.LogInformation(
                            "[FOUNDRY] Tool completed: {ToolName}. ConversationId={ConversationId}",
                            functionCall.FunctionName, conversationId);

                        inputItems.Add(ResponseItem.CreateFunctionCallOutputItem(functionCall.CallId, toolResult));
                        moreToolCalls = true;
                    }
                }
            } while (moreToolCalls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOUNDRY] Invocation failed. ConversationId={ConversationId}", conversationId);
            throw;
        }
        

        _logger.LogInformation(
            "[FOUNDRY] Response received. ConversationId={ConversationId} ToolsInvoked={ToolsInvoked}",
            conversationId, string.Join(",", toolsInvoked));

        return new AgentInvocationResult
        {
            ResponseText = response.GetOutputText(),
            LastResponseId = response.Id,
            ToolsInvoked = toolsInvoked
        };
        #pragma warning restore OPENAI001
    }

    /// <summary>
    /// Dispatches a function-call request from Foundry to the existing Phase 1
    /// tool implementations. This is the boundary the spec describes as:
    ///   Foundry Agent -> Tool -> Existing .NET Business Logic -> Result
    /// Argument names below match AppointmentTools/CallbackTools' Phase 1
    /// parameter names - adjust if your Foundry-side tool schema differs.
    /// </summary>
    private async Task<string> ExecuteToolAsync(string functionName, string argumentsJson)
    {
        try
        {
            using var args = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
            var root = args.RootElement;

            return functionName switch
            {
                "BookAppointment" => await _appointmentTools.BookAppointment(
                    root.GetProperty("patientName").GetString() ?? "",
                    root.GetProperty("mobileNumber").GetString() ?? "",
                    root.GetProperty("date").GetString() ?? "",
                    root.GetProperty("time").GetString() ?? ""),

                "RescheduleAppointment" => await _appointmentTools.RescheduleAppointment(
                    root.GetProperty("mobileNumber").GetString() ?? "",
                    root.GetProperty("newDate").GetString() ?? "",
                    root.GetProperty("newTime").GetString() ?? ""),

                "CancelAppointment" => await _appointmentTools.CancelAppointment(
                    root.GetProperty("mobileNumber").GetString() ?? ""),

                "CreateCallbackRequest" => await _callbackTools.CreateCallbackRequest(
                    root.GetProperty("name").GetString() ?? "",
                    root.GetProperty("mobileNumber").GetString() ?? "",
                    root.GetProperty("reason").GetString() ?? ""),

                _ => $"ERROR: Unknown tool '{functionName}' requested by the agent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FOUNDRY] Tool execution failed: {ToolName}", functionName);
            return $"ERROR: Tool '{functionName}' failed to execute: {ex.Message}";
        }
    }
}
