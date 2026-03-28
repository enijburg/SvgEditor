# ADR-006: Copilot-Assisted Editing via Aspire and Backend API

## Status

Accepted

## Context

Users want to describe SVG edits in natural language and have the application translate intent
into safe, previewable operations on the current document. This requires a backend orchestration
layer that can interpret prompts, generate structured commands, validate them, and return a plan
that the client previews and applies.

The editor currently runs as a standalone Blazor WebAssembly application with no backend.
Adding a backend service requires orchestration, service discovery, and observability.

## Decision

Introduce a Copilot-assisted editing flow with these components:

- **SvgEditor.AppHost** — .NET Aspire AppHost that orchestrates the client and API.
- **SvgEditor.ServiceDefaults** — shared Aspire defaults (OpenTelemetry, health checks, service discovery).
- **SvgEditor.Api** — ASP.NET Core backend with `POST /api/copilot/plan` and `POST /api/copilot/apply` endpoints.
- **Copilot panel** in the Blazor client — captures prompts, displays plan summaries, and provides apply/dismiss actions.
- **Structured command model** — deterministic `SvgCommand` hierarchy (`SetFill`, `SetStroke`, `MoveElement`, `MoveSelection`, `AlignSelection`).
- **GitHub Copilot SDK** (`GitHub.Copilot.SDK`) for natural-language understanding. The backend uses
  `CopilotClient` / `CopilotSession` with custom MCP-style tools registered via `AIFunctionFactory.Create`
  from `Microsoft.Extensions.AI`. The model interprets user prompts and calls the appropriate tools
  (e.g. `set_fill`, `move_selection`, `align_selection`) with validated parameters.
- **Version-gated apply** — the apply endpoint rejects requests when the document version has changed since planning.
- **Single-transaction undo** — all commands from one Copilot apply are grouped into a single undo entry via `PushHistoryCommand`.

Safety: the backend only allows known command types targeting known elements with validated attributes. No arbitrary XML or script injection is permitted.

## Consequences

- The editor now requires a running backend to use Copilot features; the canvas itself still works standalone.
- The Copilot CLI must be installed and authenticated for plan requests to succeed. When unavailable, the API returns a graceful error.
- Aspire orchestration adds operational visibility via the Aspire Dashboard (traces, logs, metrics).
- The command model is extensible — new command types can be added without changing the plan/apply flow.
- The tool layer can later be extracted into a standalone MCP server (`SvgEditor.McpServer`).
