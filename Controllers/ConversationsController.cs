using Microsoft.AspNetCore.Mvc;
using VoiceDentalReceptionist.Application.Interfaces;
using VoiceDentalReceptionist.Models.Requests;
using VoiceDentalReceptionist.Models.Responses;

namespace VoiceDentalReceptionist.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<ConversationCreatedResponse> CreateConversation()
    {
        var conversationId = _conversationService.CreateConversation();
        return Ok(new ConversationCreatedResponse(conversationId));
    }

    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        string conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message must not be empty.");

        try
        {
            var result = await _conversationService.SendMessageAsync(conversationId, request.Message, cancellationToken);
            return Ok(new SendMessageResponse(result.ConversationId, result.Message, result.Agent));
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Conversation '{conversationId}' was not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed. ConversationId={ConversationId}", conversationId);
            return StatusCode(500, "Something went wrong handling that message. Please try again.");
        }
    }
}
