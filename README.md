# META7 Captain M7A

META7 Captain M7A is currently a core SDK/domain simulation library written in C#. The repository models layered command, routing, safety, telemetry, and resilience behaviors as in-memory domain components rather than a deployed host or API runtime.

## Local verification

From the repository root:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

These commands restore the solution, build the core SDK project, and run deterministic smoke tests for foundational behaviors such as command gating, hash-chain validation, routing, and barrier coordination.
