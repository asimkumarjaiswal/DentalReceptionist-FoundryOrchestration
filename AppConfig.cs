namespace VoiceDentalReceptionist;

/// <summary>
/// Strongly-typed view of the four values this app needs.
/// Populated from appsettings.json and/or environment variables
/// (env vars win — see Program.cs).
/// </summary>
public class AppConfig
{
    public string FoundryProjectEndpoint { get; set; } = string.Empty;
    public string FoundryModelDeployment { get; set; } = string.Empty;
    // Name of the existing v2 Prompt Agent in Foundry (e.g. "DentalReceptionist-Phase2").
    // Per spec section 20 - never hardcode this in business logic.
    public string FoundryAgentName { get; set; } = string.Empty;
    // Region (e.g. "eastus"), not a URL. SpeechConfig.FromSubscription(key, region)
    // builds the correct internal endpoint itself - avoids the "must specify
    // WS or WSS scheme" error you get from handing FromEndpoint() the REST
    // (https://) endpoint copied off the Azure portal.
    public string AzureSpeechRegion { get; set; } = string.Empty;
    public string AzureSpeechKey { get; set; } = string.Empty;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(FoundryProjectEndpoint) &&
        !string.IsNullOrWhiteSpace(FoundryModelDeployment) &&
        !string.IsNullOrWhiteSpace(FoundryAgentName) &&
        !string.IsNullOrWhiteSpace(AzureSpeechRegion) &&
        !string.IsNullOrWhiteSpace(AzureSpeechKey);
}
