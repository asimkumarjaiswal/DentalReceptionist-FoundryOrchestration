using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;

using OpenAI.Responses;

namespace VoiceDentalReceptionist.Agents.Foundry;

public sealed class FoundryAgentProvisioner
{
    private const string AgentName = "DentalReceptionist-Phase2";

    private readonly AIProjectClient _projectClient;
    private readonly ILogger<FoundryAgentProvisioner> _logger;
    private readonly string _modelName;

    public const string Instructions = """
        You are a dental clinic receptionist assistant.

        Your responsibilities are:

        - Help patients with dental appointment requests.
        - Book new appointments when the patient provides all required information.
        - Reschedule existing appointments when requested.
        - Cancel existing appointments when requested.
        - Create a callback request when the patient needs someone from the clinic to call them back.

        Appointment booking requires:
        - Patient full name
        - Mobile number
        - Appointment date
        - Appointment time

        Rescheduling requires:
        - Patient mobile number
        - New appointment date
        - New appointment time

        Cancellation requires:
        - Patient mobile number

        Callback requests require:
        - Patient name
        - Mobile number
        - Reason for the callback

        Important rules:

        - Ask the patient for missing information before calling a tool.
        - Do not invent patient information.
        - Do not claim an appointment was booked, rescheduled, or cancelled unless the corresponding tool returns a successful result.
        - If a tool returns an ERROR, clearly explain the problem to the patient and ask for the required information or offer an appropriate next step.
        - Keep responses concise and conversational because this assistant is used in a voice-first experience.
        - Confirm important appointment details before booking whenever appropriate.
        - Use the available tools for appointment operations rather than pretending to perform them yourself.
        """;

    public FoundryAgentProvisioner(
        AIProjectClient projectClient,
        AppConfig config,
        ILogger<FoundryAgentProvisioner> logger)
    {
        _projectClient = projectClient;
        _modelName = config.FoundryModelDeployment;
        _logger = logger;
    }

#pragma warning disable OPENAI001

    public async Task<string> EnsureAgentAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FOUNDRY] Checking whether agent {AgentName} exists.",
            AgentName);

        var agentExists = await AgentExistsAsync(cancellationToken);

        if (agentExists)
        {
            _logger.LogInformation( "[FOUNDRY] Agent {AgentName} already exists. No provisioning required.",AgentName);

            return AgentName;
        }

        _logger.LogInformation("[FOUNDRY] Agent {AgentName} does not exist. Creating agent.",AgentName);

        return await CreateAgentAsync(cancellationToken);
    }

    private async Task<bool> AgentExistsAsync(CancellationToken cancellationToken)
    {
        await foreach (
            var agent in _projectClient.AgentAdministrationClient.GetAgentsAsync(cancellationToken: cancellationToken))
        {
            if (string.Equals(agent.Name,AgentName,StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string> CreateAgentAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[FOUNDRY] Creating agent {AgentName} using model {ModelName}.",AgentName, _modelName);

        var agentDefinition = new DeclarativeAgentDefinition(model: _modelName)
            {
                Instructions = Instructions,

                Tools =
                {
                    CreateBookAppointmentTool(),
                    CreateRescheduleAppointmentTool(),
                    CreateCancelAppointmentTool(),
                    CreateCallbackRequestTool()
                }
            };

        var createResult =await _projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                    agentName: AgentName,
                    options: new(agentDefinition),
                    cancellationToken: cancellationToken);

        var newVersion = createResult.Value;

        _logger.LogInformation(
            "[FOUNDRY] Agent created successfully. Agent={AgentName}, Version={Version}",
            AgentName,
            newVersion.Version);

        return AgentName;
    }

    private static FunctionTool CreateBookAppointmentTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "BookAppointment",

            functionDescription:
                "Books a new dental appointment for a patient. " +
                "Requires the patient's full name, mobile number, date, and time.",

            functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",

                    properties = new
                    {
                        patientName = new
                        {
                            type = "string",
                            description =
                                "Full name of the patient"
                        },

                        mobileNumber = new
                        {
                            type = "string",
                            description =
                                "Patient's mobile number"
                        },

                        date = new
                        {
                            type = "string",
                            description =
                                "Requested appointment date, " +
                                "for example '18th Aug 2026'"
                        },

                        time = new
                        {
                            type = "string",
                            description =
                                "Requested appointment time, " +
                                "for example '5 PM'"
                        }
                    },

                    required = new[]
                    {
                        "patientName",
                        "mobileNumber",
                        "date",
                        "time"
                    }
                }),

            strictModeEnabled: false);
    }

    private static FunctionTool CreateRescheduleAppointmentTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "RescheduleAppointment",

            functionDescription:
                "Reschedules an existing dental appointment using " +
                "the patient's mobile number.",

            functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",

                    properties = new
                    {
                        mobileNumber = new
                        {
                            type = "string",
                            description =
                                "Mobile number used when the appointment was booked"
                        },

                        newDate = new
                        {
                            type = "string",
                            description =
                                "New appointment date"
                        },

                        newTime = new
                        {
                            type = "string",
                            description =
                                "New appointment time"
                        }
                    },

                    required = new[]
                    {
                        "mobileNumber",
                        "newDate",
                        "newTime"
                    }
                }),

            strictModeEnabled: false);
    }

    private static FunctionTool CreateCancelAppointmentTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "CancelAppointment",

            functionDescription:
                "Cancels an existing dental appointment using " +
                "the patient's mobile number.",

            functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",

                    properties = new
                    {
                        mobileNumber = new
                        {
                            type = "string",
                            description =
                                "Mobile number used when the appointment was booked"
                        }
                    },

                    required = new[]
                    {
                        "mobileNumber"
                    }
                }),

            strictModeEnabled: false);
    }

    private static FunctionTool CreateCallbackRequestTool()
    {
        return ResponseTool.CreateFunctionTool(
            functionName: "CreateCallbackRequest",

            functionDescription:
                "Creates a callback request when the dental clinic " +
                "needs to call the patient back.",

            functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    type = "object",

                    properties = new
                    {
                        name = new
                        {
                            type = "string",
                            description =
                                "Full name of the person requesting the callback"
                        },

                        mobileNumber = new
                        {
                            type = "string",
                            description =
                                "Mobile number for the callback"
                        },

                        reason = new
                        {
                            type = "string",
                            description =
                                "Reason for requesting the callback"
                        }
                    },

                    required = new[]
                    {
                        "name",
                        "mobileNumber",
                        "reason"
                    }
                }),

            strictModeEnabled: false);
    }

#pragma warning restore OPENAI001
}