using Microsoft.CognitiveServices.Speech;

namespace VoiceDentalReceptionist.Voice;

/// <summary>
/// Step 8: wraps Azure AI Speech's SpeechRecognizer for single-utterance
/// recognition from the default microphone. Deliberately just one method -
/// listen once, return text or null. No streaming, no continuous
/// recognition; Phase 1 only needs "the caller says something, we get text".
/// </summary>
public class SpeechToTextService
{
    private readonly SpeechConfig _speechConfig;

    public SpeechToTextService(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.AzureSpeechRegion) || string.IsNullOrWhiteSpace(config.AzureSpeechKey))
            throw new InvalidOperationException("AZURE_SPEECH_REGION / AZURE_SPEECH_KEY are not set.");

        _speechConfig = SpeechConfig.FromSubscription(config.AzureSpeechKey, config.AzureSpeechRegion);
        _speechConfig.SpeechRecognitionLanguage = "en-US";
    }

    /// <summary>
    /// Listens for one utterance from the default microphone and returns the
    /// recognized text, or null if nothing usable was recognized.
    /// </summary>
    public async Task<string?> ListenOnceAsync()
    {
        using var audioConfig = Microsoft.CognitiveServices.Speech.Audio.AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

        Console.WriteLine("[VOICE] Listening...");
        var result = await recognizer.RecognizeOnceAsync();

        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Console.WriteLine($"[STT] Transcription: \"{result.Text}\"");
                return result.Text;

            case ResultReason.NoMatch:
                Console.WriteLine("[STT] No speech could be recognized.");
                return null;

            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"[STT] Recognition canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
                return null;

            default:
                return null;
        }
    }
}
