# Felix

Framework for Efficient Living & Intelligent eXecution

A personal assistant system backend API built with C# / .NET 8.

## Project Structure

```
Felix.slnx
├── src/
│   ├── Felix.Api/              # API layer (entry point)
│   ├── Felix.Domain/           # Business logic (Services)
│   ├── Felix.Infrastructure/   # Data layer + External API calls
│   └── Felix.Common/           # Shared components
│
└── tests/
    ├── Felix.Domain.Tests/
    └── Felix.Api.Tests/
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

## Health Check

```bash
curl http://localhost:5000/health
```
