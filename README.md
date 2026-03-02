# Felix

Framework for Efficient Living & Intelligent eXecution

A personal AI assistant backend built with .NET 10 + Semantic Kernel + Gemini.

## Features

- AI Assistant with Gemini (gemini-2.5-flash)
- MCP Client integration for external tools
- Taiwan weather via Central Weather Administration API
- International weather via MCP Server (Open-Meteo)
- API Key rotation for rate limit handling

## Architecture

```
User Request
     │
     ▼
┌───────────────────────────────────┐
│  AI: Taiwan or International?     │
└───────────────────────────────────┘
     │                │
     ▼                ▼
┌─────────────────┐  ┌─────────────────┐
│  Taiwan         │  │  International  │
│  TaiwanWeather  │  │  weather-mcp    │
│  Tool (Local)   │  │  (Open-Meteo)   │
└─────────────────┘  └─────────────────┘
```

## Project Structure

```
Felix.slnx
├── src/
│   ├── Felix.Api/              # API entry point
│   ├── Felix.Domain/           # Business logic
│   ├── Felix.Infrastructure/   # AI, MCP, External APIs
│   └── Felix.Common/           # Shared components
│
└── tests/
    ├── Felix.Domain.Tests/
    └── Felix.Api.Tests/
```

## Configuration

```json
{
  "Gemini": {
    "Model": "gemini-2.5-flash",
    "ApiKeys": ["key1", "key2", "key3"]
  },
  "CwaApiKey": "your-central-weather-api-key",
  "Mcp": {
    "Servers": [
      {
        "Name": "weather",
        "Command": "npx",
        "Arguments": ["-y", "@anthropic-ai/weather-mcp"],
        "Enabled": true
      }
    ]
  }
}
```

## Getting Started

```bash
# Build
dotnet build

# Run
dotnet run --project src/Felix.Api

# Test
dotnet test
```

## API Endpoints

```bash
# Health check
curl http://localhost:3080/health

# Ask Felix
curl -X POST http://localhost:3080/api/v1/assistant/process \
  -H "Content-Type: application/json" \
  -d '{"message": "台北天氣如何"}'

# Check API key status
curl http://localhost:3080/api/v1/assistant/status
```
