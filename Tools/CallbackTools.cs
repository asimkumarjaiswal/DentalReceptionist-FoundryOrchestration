using System.ComponentModel;
using VoiceDentalReceptionist.Storage;

namespace VoiceDentalReceptionist.Tools;

/// <summary>
/// Step 11: the simple human-in-the-loop mechanism. No real phone transfer -
/// this just logs a callback request to disk. Owned directly by the
/// Receptionist Agent (not delegated to the Appointment Agent), since a
/// handoff request isn't necessarily appointment-related.
/// </summary>
public class CallbackTools
{
    private readonly JsonStorage<CallbackRequest> _storage;

    public CallbackTools(string callbackFilePath)
    {
        _storage = new JsonStorage<CallbackRequest>(callbackFilePath);
    }

    [Description("Logs a request for a human receptionist to call the patient back. Use this when the caller asks for a human, or when their request can't reasonably be handled by the AI.")]
    public async Task<string> CreateCallbackRequest(
        [Description("Caller's full name")] string name,
        [Description("Caller's mobile number")] string mobileNumber,
        [Description("Short reason for the callback")] string reason)
    {
        Console.WriteLine($"[TOOL] CreateCallbackRequest called: {name}, {mobileNumber}, reason='{reason}'");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(mobileNumber))
        {
            return "ERROR: Missing name or mobile number. Ask the caller for whichever is missing before logging the callback.";
        }

        var request = new CallbackRequest
        {
            Name = name,
            MobileNumber = mobileNumber,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason
        };

        await _storage.AppendAsync(request);
        Console.WriteLine($"[TOOL] Callback request logged: {request.Id}");

        return "I've recorded your request. A receptionist will call you back.";
    }
}
