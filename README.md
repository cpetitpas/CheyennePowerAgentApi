# CheyennePowerAgentApi

ASP.NET Core 10 Web API for real-time operations monitoring of a natural gas-fired power
generation facility. An AI agent (Claude) analyzes telemetry events; a tool layer collects
generation state and enables deterministic dispatch decisions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An Anthropic API key

## Running locally

Set your API key:

```powershell
# PowerShell
$env:Anthropic__ApiKey = "sk-ant-..."
```
```bash
# bash / zsh
export Anthropic__ApiKey="sk-ant-..."
```

Then from the repo root:

```bash
dotnet run --project src/CheyennePowerAgentApi
```

Open `http://localhost:5255` to view the operations dashboard.

The `Anthropic__ApiKey` environment variable is required for Claude-powered endpoints
(alarm analysis, flow analysis, turbine analysis, and agentic investigation). The generation
dispatch endpoint and tool health endpoints do not require it.

## Running tests

From the repo root:

```bash
# All tests (requires Anthropic__ApiKey for integration tests)
dotnet test

# Non-integration only — no API key required
dotnet test --filter "Category!=Integration"

# Integration tests only
dotnet test --filter "Category=Integration"
```

42 unit/contract tests and 4 integration tests covering endpoint contracts, input validation,
tool envelope shape, per-tool payload values, degraded-tool escalation, agentic investigation
loop, multi-node synthesis, and incident chat against a real Claude response.

## Features

- **Generator alarm analysis** — Claude-powered assessment of gas turbine alarms (`POST /api/generator/analyze`)
- **Flow analysis** — AI analysis of fuel gas flow anomalies (`POST /api/flow/analyze`)
- **Turbine alarm analysis** — Structured derate recommendations (`POST /api/turbine/analyze`)
- **Generation dispatch** — Deterministic dispatch gap calculation with fuel-cell, gas supply,
  emissions, and load-forecast inputs (`POST /api/generation/dispatch`)
- **Tool health endpoints** — Direct inspection of each generation data tool via `ToolResult<T>`
  envelope (`GET /api/tools/*`)
- **Real-time SSE dashboard** — `dashboard.html` served at `/`; streams ALARM, FLOW,
  TURBINE_ALARM, DISPATCH, and correlated multi-node alarm events via `GET /api/stream/events`
- **Tool health tab** — Live view of Status, Confidence, Source, and Staleness for all five
  generation tools
- **Investigate workflow** — Single-node and multi-node agentic investigations with a five-tool
  loop (generator spec, recent telemetry, maintenance history, fuel supply status, output
  thresholds), per-node severity, root cause hypothesis, correlation summary, and an integrated
  incident chat panel that opens automatically after each investigation
- **Copy-to-investigate** — Each Live Feed event card has a ⎘ button that copies the event,
  switches to the Investigate tab, pre-selects Single or Multi Node mode, and pastes the alarm
  text ready to parse. The feed auto-pauses when scrolled to prevent cards shifting under the
  cursor. DISPATCH events are identified as informational and the affected node is pre-selected
  in the form instead
- **Robust Claude response parsing** — Handles mixed prose + JSON responses from Claude by
  extracting structured investigation data even when the model includes explanatory text or
  fenced code blocks

## Investigate workflow

Events copied from the Live Feed use the format:

EVENT_TYPE NODE_ID SEVERITY
<analysis text>
→ <action text>


Paste this into the Investigate tab and press **Parse Alarm** (Single Node) or
**Parse into Multi Node** (Multi Node) to populate the form fields before running
an investigation.

## Tool layer

All generation data calls go through `ToolExecutor`, which provides per-attempt timeout (3 s),
automatic retry (up to 2 retries), and a safe conservative fallback on exhaustion. Responses
are wrapped in a `ToolResult<T>` envelope carrying `status`, `confidence`, `source`,
`stale_after_seconds`, `timestamp`, and `fallback_reason`.