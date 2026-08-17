using System.Collections.Concurrent;

namespace VoiceDentalReceptionist.Application;

/// <summary>
/// Simple in-memory conversation store per spec section 13 ("do not introduce
/// a database unless required"). Conversations are lost on app restart -
/// acceptable for this project's scope; swap for a persisted store later if
/// needed without touching anything above IConversationStore.
/// </summary>
public class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, string?> _conversations = new();

    public string CreateConversation()
    {
        var conversationId = Guid.NewGuid().ToString("N")[..12];
        _conversations[conversationId] = null;
        return conversationId;
    }

    public bool Exists(string conversationId) => _conversations.ContainsKey(conversationId);

    public string? GetLastResponseId(string conversationId) =>
        _conversations.TryGetValue(conversationId, out var responseId) ? responseId : null;

    public void SetLastResponseId(string conversationId, string responseId) =>
        _conversations[conversationId] = responseId;
}
