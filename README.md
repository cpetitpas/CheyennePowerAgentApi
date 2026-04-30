# CheyennePowerAgentApi

ASP.NET Core 10 Web API for real-time operations monitoring of a natural gas-fired power generation facility. An AI agent (Claude) analyzes telemetry events; a tool layer collects generation state and enables deterministic dispatch decisions.

## Features

- **Generator alarm analysis** — Claude-powered assessment of gas turbine alarms (`POST /api/generator/analyze`)
- **Flow analysis** — AI analysis of fuel gas flow anomalies (`POST /api/flow/analyze`)
- **Turbine alarm analysis** — Structured derate recommendations (`POST /api/turbine/analyze`)
- **Generation dispatch** — Deterministic dispatch gap calculation with fuel-cell, gas supply, emissions, and load-forecast inputs (`POST /api/generation/dispatch`)
- **Tool health endpoints** — Direct inspection of each generation data tool via `ToolResult<T>` envelope (`GET /api/tools/*`)
- **Real-time SSE dashboard** — `dashboard.html` served at `/`; streams ALARM, FLOW, TURBINE_ALARM, and DISPATCH events via `GET /api/stream/events`
- **Tool health tab** — Live view of Status, Confidence, Source, and Staleness for all five generation tools

## Tool layer

All generation data calls go through `ToolExecutor`, which provides per-attempt timeout (3 s), automatic retry (up to 2 retries), and a safe conservative fallback on exhaustion. Responses are wrapped in a `ToolResult<T>` envelope carrying `status`, `confidence`, `source`, `stale_after_seconds`, `timestamp`, and `fallback_reason`.

## Running locally

```bash
cd src/CheyennePowerAgentApi
dotnet run
```

Open `http://localhost:5255` in a browser to view the operations dashboard.

The `ANTHROPIC_API_KEY` environment variable must be set for Claude-powered endpoints (alarm analysis, flow analysis, turbine analysis). The generation dispatch endpoint and tool health endpoints do not require it.

## Running tests

```bash
dotnet test
```

20 integration tests covering endpoint contracts, input validation, tool envelope shape, per-tool payload values, and the degraded-tool escalation path.