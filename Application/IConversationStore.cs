namespace VoiceDentalReceptionist.Application;

/// <summary>
/// Maps a public conversationId to the last Foundry response Id for that
/// conversation - that's all continuity requires with the Responses API
/// (no explicit Thread object needed, unlike Classic/Persistent Agents).
/// </summary>
public interface IConversationStore
{
    string CreateConversation();
    bool Exists(string conversationId);
    string? GetLastResponseId(string conversationId);
    void SetLastResponseId(string conversationId, string responseId);
}
