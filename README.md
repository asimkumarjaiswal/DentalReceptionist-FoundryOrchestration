# Voice Dental Receptionist — Phase 2

A learning/portfolio project demonstrating a **Foundry-hosted AI agent integrated
with an ASP.NET Core Web API and local function tools**.

Phase 1 implemented the dental receptionist capabilities as local .NET tools
and local agent orchestration.

Phase 2 moves the agent reasoning and tool selection to **Microsoft Foundry**.
The application invokes the Foundry-hosted agent
**DentalReceptionist-Phase2** through the Azure AI Projects Responses API.

The four business functions remain implemented locally in .NET because they
operate on the application's local JSON data files.

See `TESTING.md` for the step-by-step test plan.

---

## Phase 2 goals

Phase 2 demonstrates the following architecture:

- Microsoft Foundry-hosted agent
- Agent provisioning from .NET startup
- Foundry-managed function definitions
- LLM-driven function/tool selection
- Local .NET function execution
- Function results submitted back to Foundry
- Multi-turn conversation support
- ASP.NET Core Web API
- Reuse of Phase 1 business logic
- Local JSON persistence
- Azure authentication using `DefaultAzureCredential` / Azure CLI

The goal is to demonstrate the boundary between:

**AI reasoning / tool selection**

and

**application-owned business logic / data access**.

---

# Architecture

```text
                    Client / Voice Application
                              |
                              v
             POST /api/conversations/{id}/messages
                              |
                              v
                  ConversationsController
                              |
                              v
                    ConversationService
                              |
                              v
                  AgentOrchestrationService
                              |
                              v
                    FoundryAgentService
                              |
                              v
              Microsoft Foundry Responses API
                              |
                              v
              DentalReceptionist-Phase2 Agent
                              |
                ┌─────────────┴──────────────┐
                |                            |
         Normal response              Function call
                                             |
                                             v
                                  FoundryAgentService
                                             |
                                             v
                                      ExecuteToolAsync()
                                             |
                         ┌───────────────────┼───────────────────┐
                         |                   |                   |
                         v                   v                   v
                 AppointmentTools     CallbackTools       Local business
                         |                   |                logic
                         └───────────────────┼──────────────────┘
                                             |
                                             v
                                      data/*.json
                                             |
                                             v
                                  Function result returned
                                  back to Foundry
                                             |
                                             v
                                      Final response
                                             |
                                             v
                                   ASP.NET Core API