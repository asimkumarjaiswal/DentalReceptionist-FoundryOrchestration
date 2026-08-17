namespace VoiceDentalReceptionist.Models.Responses;

public record HealthResponse(string Status);

public record ConversationCreatedResponse(string ConversationId);

public record SendMessageResponse(string ConversationId, string Message, string Agent);
