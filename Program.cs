using Azure.AI.Projects;
using Azure.Identity;
using VoiceDentalReceptionist;
using VoiceDentalReceptionist.Agents.Foundry;
using VoiceDentalReceptionist.Application;
using VoiceDentalReceptionist.Application.Interfaces;
using VoiceDentalReceptionist.Application.Services;
using VoiceDentalReceptionist.Tools;
using VoiceDentalReceptionist.Voice;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -----------------------------------------------------
var config = new AppConfig
{
    FoundryProjectEndpoint = builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"]
        ?? builder.Configuration["Foundry:ProjectEndpoint"] ?? string.Empty,
    FoundryModelDeployment = builder.Configuration["FOUNDRY_MODEL_DEPLOYMENT"]
        ?? builder.Configuration["Foundry:ModelDeployment"] ?? string.Empty,
    FoundryAgentName = builder.Configuration["FOUNDRY_AGENT_NAME"]
        ?? builder.Configuration["Foundry:AgentName"] ?? string.Empty,
    AzureSpeechRegion = builder.Configuration["AZURE_SPEECH_REGION"]
        ?? builder.Configuration["AzureSpeech:Region"] ?? string.Empty,
    AzureSpeechKey = builder.Configuration["AZURE_SPEECH_KEY"]
        ?? builder.Configuration["AzureSpeech:Key"] ?? string.Empty,
};

if (!config.IsComplete)
{
    Console.WriteLine("[CONFIG] Missing one or more required values. Required:");
    Console.WriteLine("[CONFIG]   FOUNDRY_PROJECT_ENDPOINT, FOUNDRY_MODEL_DEPLOYMENT, FOUNDRY_AGENT_NAME,");
    Console.WriteLine("[CONFIG]   AZURE_SPEECH_REGION, AZURE_SPEECH_KEY");
    // Fail fast rather than starting an API that can't actually talk to Foundry.
    throw new InvalidOperationException("Application configuration is incomplete - see console output above.");
}

builder.Services.AddSingleton(config);

// ---- Data paths (reused from Phase 1's project-relative approach) ------
var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
var projectDir = baseDir.Parent?.Parent?.Parent?.FullName
    ?? throw new InvalidOperationException("Could not determine project directory.");
var dataDir = Path.Combine(projectDir, "data");
var appointmentsPath = Path.Combine(dataDir, "appointments.json");
var callbackPath = Path.Combine(dataDir, "callback-requests.json");

#region  Phase 2 Implementation
builder.Services.AddSingleton<AIProjectClient>(sp =>
{
    var config = sp.GetRequiredService<AppConfig>();

    return new AIProjectClient(new Uri(config.FoundryProjectEndpoint),new AzureCliCredential());
});

builder.Services.AddSingleton<FoundryAgentProvisioner>();

#endregion


// ---- Tools (Phase 1, reused as-is - spec section 18) --------------------
builder.Services.AddSingleton(sp => new AppointmentTools(appointmentsPath));
builder.Services.AddSingleton(sp => new CallbackTools(callbackPath));

// ---- Foundry integration boundary (spec section 12) ----------------------
builder.Services.AddSingleton<FoundryAgentService>();

// ---- Conversation state (spec section 13) --------------------------------
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();

// ---- Application layer (spec section 11) ---------------------------------
builder.Services.AddScoped<IAgentOrchestrationService, AgentOrchestrationService>();
builder.Services.AddScoped<IConversationService, ConversationService>();

// ---- Speech services (spec section 19 - kept intact, not wired into the
// synchronous HTTP path; available for a future voice UI to call directly) --
builder.Services.AddSingleton(sp => new SpeechToTextService(config));
builder.Services.AddSingleton(sp => new TextToSpeechService(config));

// ---- Web API host ---------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#region Phase 2 Scope
using (var scope = app.Services.CreateScope())
{
    var provisioner = scope.ServiceProvider.GetRequiredService<FoundryAgentProvisioner>();
    await provisioner.EnsureAgentAsync();
}

#endregion

app.MapControllers();

Console.WriteLine("[STARTUP] Voice Dental Receptionist API - Phase 2");
Console.WriteLine($"[STARTUP] Foundry agent: {config.FoundryAgentName}");
Console.WriteLine("[STARTUP] Swagger UI available at /swagger in Development.");

app.Run();
