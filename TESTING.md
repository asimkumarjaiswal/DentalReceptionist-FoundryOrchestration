# Phase 2 — Test Checklist

Run with `dotnet run`, then use Swagger UI (opens automatically at
`/swagger` in Development) or `curl`/Postman against
`http://localhost:5080` (per `Properties/launchSettings.json`).

## 1. Build verification (non-negotiable per spec section 29)
- [ ] `dotnet clean` succeeds
- [ ] `rm -rf bin obj`
- [ ] `dotnet restore` succeeds — check `dotnet list package --include-transitive`
      confirms `OpenAI` resolved to **2.9.1** (not 2.12.x — if something
      else in the dependency graph is forcing it back up to 2.12.x, that
      package is reintroducing the exact conflict we just removed)
- [ ] `dotnet build` — expect **0 errors**
- [ ] `dotnet run` — app starts, **no `MissingMethodException`** on
      `OpenAI.Responses.ResponsesClient..ctor` at startup or on the first
      message call. This was root-caused via reflection to
      `Azure.AI.Extensions.OpenAI`'s compiled IL expecting an older
      `ResponsesClient` constructor shape than `OpenAI 2.12.0` provided —
      fixed by removing `Microsoft.Agents.AI`/`Microsoft.Extensions.AI.*`
      (which forced `OpenAI >= 2.12.0`) and pinning `OpenAI` to `2.9.1`
      directly. If this exception reappears after a package change, it
      means something re-pinned `OpenAI` above what
      `Azure.AI.Extensions.OpenAI` was actually built against.
- [ ] If you still hit a build error inside `FoundryAgentService.cs`
      specifically (not the `MissingMethodException` above, an actual
      **compile** error), check the one remaining open item flagged in its
      doc comment — whether `DentalReceptionist-Phase2`'s configured tool
      schema uses the same function/argument names `ExecuteToolAsync`
      expects. The SDK type/property names themselves are already confirmed
      against Microsoft's own sample code, so a compile error there is more
      likely a schema mismatch than an SDK surface guess gone wrong.

## 2. Config sanity
- [ ] Run with no env vars / empty `appsettings.json` -> app logs which
      values are missing and throws before `app.Run()` (fail-fast, not a
      silently broken API)
- [ ] Set all five required values (`FOUNDRY_PROJECT_ENDPOINT`,
      `FOUNDRY_MODEL_DEPLOYMENT`, `FOUNDRY_AGENT_NAME`, `AZURE_SPEECH_REGION`,
      `AZURE_SPEECH_KEY`) -> app starts cleanly, logs
      `[STARTUP] Foundry agent: DentalReceptionist-Phase2`

## 3. Health endpoint
- [ ] `GET /api/health` -> `200 OK`, `{ "status": "Healthy" }`
- [ ] This should work even if Foundry/Speech credentials are wrong — health
      doesn't touch either

## 4. Swagger
- [ ] Navigate to `/swagger` -> both `ConversationsController` and
      `HealthController` endpoints are listed and callable from the UI

## 5. Conversation creation
- [ ] `POST /api/conversations` (empty body) -> `200 OK`,
      `{ "conversationId": "<12-char id>" }`
- [ ] Two separate calls return two different `conversationId` values

## 6. First message + Foundry auth
- [ ] `POST /api/conversations/{id}/messages` with
      `{ "message": "Hi, I'd like to book an appointment." }`
- [ ] `200 OK` with a `SendMessageResponse` — `agent` field should read
      `DentalReceptionist-Phase2`
- [ ] If this 401s, it's the same Foundry-auth class of issue from earlier —
      re-check `az login`, the **Foundry User** role assignment, and that
      `FOUNDRY_AGENT_NAME` exactly matches the agent's name in the portal

## 7. Booking flow (tool round-trip)
- [ ] Continue the same conversation: "Tomorrow at 5 PM, my name is ... and my number is ..."
- [ ] Console logs show `[FOUNDRY] Tool invoked: BookAppointment` then
      `[FOUNDRY] Tool completed: BookAppointment`
- [ ] `data/appointments.json` now contains the new appointment,
      `Status="Booked"`
- [ ] The response text is a natural confirmation, not raw JSON or an error

## 8. Reschedule / cancel flow
- [ ] Same conversation: "Actually move it to Friday at 4 PM" ->
      `[FOUNDRY] Tool invoked: RescheduleAppointment` in the logs, entry in
      `appointments.json` updated to `Status="Rescheduled"`
- [ ] "Please cancel it" -> `CancelAppointment` tool call, entry updated to
      `Status="Cancelled"`

## 9. Human handoff
- [ ] "I want to speak to a human" -> `[FOUNDRY] Tool invoked: CreateCallbackRequest`
- [ ] `data/callback-requests.json` has a new entry

## 10. Multi-turn continuity
- [ ] Send a message giving your name in turn 1, then in turn 2 (same
      `conversationId`) ask "what's my name?" -> the agent recalls it,
      proving the `previousResponseId` chain in `IConversationStore` is
      actually carrying context across HTTP requests, not just within one
      call

## 11. Unknown conversation
- [ ] `POST /api/conversations/does-not-exist/messages` -> `404 Not Found`,
      not a 500 or an unhandled exception

## 12. Missing message
- [ ] `POST /api/conversations/{id}/messages` with `{ "message": "" }` ->
      `400 Bad Request`

## 13. Delegation (if configured in Foundry)
- [ ] If `DentalReceptionist-Phase2` has Connected Agents wired to an
      Appointment/Callback capability in the Foundry portal, confirm the
      response quality suggests delegation happened (e.g. it asks
      appointment-specific follow-ups) — this hop is invisible in our logs
      by design, since it happens server-side in Foundry, not in
      `FoundryAgentService`

## What "working" looks like

By the end of this checklist you should be able to point at:
- The build output (0 errors, no `MissingMethodException` at runtime)
- The API logs, turn by turn: message received -> Foundry invocation
  started -> (tool invoked/completed, if any) -> response received ->
  request completed
- The actual `data/*.json` files changing as a result of tool calls that
  originated from the Foundry agent's decision, not from any `if (intent == ...)`
  in this codebase
