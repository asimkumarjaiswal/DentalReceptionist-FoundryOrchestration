using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace VoiceDentalReceptionist.Voice;

/// <summary>
/// Step 9: wraps Azure AI Speech's SpeechSynthesizer to speak text out loud
/// through the default speaker. Mirrors SpeechToTextService's simplicity -
/// one method, fire-and-await.
/// </summary>
public class TextToSpeechService
{
    private readonly SpeechConfig _speechConfig;

    public TextToSpeechService(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.AzureSpeechRegion) || string.IsNullOrWhiteSpace(config.AzureSpeechKey))
            throw new InvalidOperationException("AZURE_SPEECH_REGION / AZURE_SPEECH_KEY are not set.");

        _speechConfig = SpeechConfig.FromSubscription(config.AzureSpeechKey, config.AzureSpeechRegion);
        _speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural";
    }

    public async Task SpeakAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Console.WriteLine("[TTS] Generating response...");

        using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

        var result = await synthesizer.SpeakTextAsync(text);

        if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
            Console.WriteLine($"[TTS] Synthesis canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
        }
    }
}
