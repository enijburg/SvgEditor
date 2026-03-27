# ADR-006: Copilot-Assisted Editing via Aspire and Backend API

## Status

Accepted

## Context

Users want to describe SVG edits in natural language and have the application translate intent
into safe, previewable operations on the current document. This requires a backend orchestration
layer that can parse prompts, generate structured commands, validate them, and return a plan that
the client previews and applies.

The editor currently runs as a standalone Blazor WebAssembly application with no backend.
Adding a backend service requires orchestration, service discovery, and observability.

## Decision

Introduce a Copilot-assisted editing flow with these components:

- **SvgEditor.AppHost** — .NET Aspire AppHost that orchestrates the client and API.
- **SvgEditor.ServiceDefaults** — shared Aspire defaults (OpenTelemetry, health checks, service discovery).
- **SvgEditor.Api** — ASP.NET Core backend with `POST /api/copilot/plan` and `POST /api/copilot/apply` endpoints.
- **Copilot panel** in the Blazor client — captures prompts, displays plan summaries, and provides apply/dismiss actions.
- **Structured command model** — deterministic `SvgCommand` hierarchy (`SetFill`, `SetStroke`, `MoveElement`, `MoveSelection`, `AlignSelection`).
- **MCP-style tool layer** — internal tool abstractions (`GetSelection`, `GetDocumentSummary`, `SetFill`, `MoveSelection`, `AlignSelection`) for future extensibility.
- **Rule-based prompt parser** for the first slice (no LLM dependency).
- **Version-gated apply** — the apply endpoint rejects requests when the document version has changed since planning.
- **Single-transaction undo** — all commands from one Copilot apply are grouped into a single undo entry via `PushHistoryCommand`.

Safety: the backend only allows known command types targeting known elements with validated attributes. No arbitrary XML or script injection is permitted.

## Consequences

- The editor now requires a running backend to use Copilot features; the canvas itself still works standalone.
- Aspire orchestration adds operational visibility via the Aspire Dashboard (traces, logs, metrics).
- The command model is extensible — new command types can be added without changing the plan/apply flow.
- The rule-based parser can later be replaced with an LLM-backed orchestrator behind the same endpoint.
- The tool layer can later be extracted into a standalone MCP server (`SvgEditor.McpServer`).
