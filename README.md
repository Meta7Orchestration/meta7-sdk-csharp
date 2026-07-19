# META7 C# SDK

> จดจำไว้ แล้วไปด้วยกัน — *Remember, and go together*

## Overview

The META7 C# SDK is a multi-layer operator intelligence platform.  
This repository contains two major components:

| Component | Description |
|-----------|-------------|
| **META7.CaptainM7A SDK** | Core multi-layer strategic intelligence SDK (Layers 1–30) |
| **META7.Operator.Api** | Operator execution layer with read-only community integration |

---

## Community Connector Layer (Read-Only)

The **META7 Community Connector Layer** is a controlled, deterministic, read-only integration layer that allows the Embodied Operator to connect to external communities (Facebook Groups, LINE OpenChat, Discord Channels, Marketplace pages, etc.) **without performing any write actions**.

### Architecture

```
META7.Operator.Api/
├── Community/
│   ├── CommunityConnector.cs          ← Read-only community scanner
│   ├── CommunityDiscoveryEngine.cs    ← Identifies type, visibility, activity
│   ├── CommunityContextExtractor.cs   ← Converts raw data to structured context
│   ├── CommunitySafetyGate.cs         ← Domain allowlist + SAFE_LOCK enforcement
│   └── Models/
│       ├── CommunityMetadata.cs       ← Full metadata model
│       ├── CommunityPost.cs           ← Public post model
│       ├── CommunityThread.cs         ← Reply thread model
│       └── CommunitySignal.cs         ← Analytical signal model
├── DirectiveExecutionService.cs       ← Routes actions to connectors
├── IPlaywrightOperatorExecutor.cs     ← Read-only browser abstraction
└── OperatorActionType.cs              ← ScanCommunity, ExtractCommunityContext

META7.Operator.Contracts/
└── Community/
    ├── CommunityScanRequest.cs        ← Input contract
    ├── CommunityScanResult.cs         ← Output contract
    └── CommunityContext.cs            ← Structured analytical context
```

### Read-Only Guarantees

The Community Connector Layer enforces strict read-only behaviour at multiple levels:

1. **`IPlaywrightOperatorExecutor`** — Only navigation and content extraction methods are exposed. No input, click, form submission, or messaging methods exist on the interface.

2. **`CommunitySafetyGate`** — Three safety invariants are enforced before any navigation:
   - **Domain allowlist** — Only approved community hosts are allowed (Facebook, Discord, LINE).
   - **SAFE_LOCK** — All operations are blocked when `ISafeLockProvider.IsSafeLockActive` is true.
   - **Login-required / private pages** — Navigation to gated pages is blocked immediately.

3. **`CommunityConnector`** — Performs a post-navigation safety check after every page load to detect login redirects and private-page redirects.

4. **`DirectiveExecutionService`** — Enforces SAFE_LOCK at the directive level before dispatching any action.

### Supported Community Types

| Platform | Example URL Pattern |
|----------|---------------------|
| Facebook Group | `https://www.facebook.com/groups/<id>` |
| LINE OpenChat | `https://openchat.line.me/openchat/discover` |
| Discord Channel | `https://discord.com/channels/<guild>/<channel>` |
| Marketplace | `https://marketplace.meta.com/...` |

### Extracted Data (Public Only)

- Group metadata (name, description, member count if public)
- Recent posts (public only, max configurable)
- Thread structure (depth-bounded)
- Post timestamps
- Visible reaction counts (read-only, aggregated)

### Community Context Extraction

`CommunityContextExtractor` converts raw scan data into a structured `CommunityContext` that identifies:

| Signal Type | Description |
|-------------|-------------|
| **Pain Points** | Keywords indicating user problems or frustrations |
| **Recurring Issues** | Patterns appearing across multiple posts |
| **Trending Topics** | High-reaction posts indicating popular topics |
| **Urgency Signals** | Keywords indicating time-sensitive or critical needs |

> **Note:** Context extraction does **not** generate outreach messages. That capability is reserved for a future PR (Autonomous Revenue Engine).

### SAFE_LOCK Integration

The `ISafeLockProvider` interface allows the safety gate to query lock status without coupling to any specific implementation. When `IsSafeLockActive` returns `true`:

- All navigation is blocked
- All directive execution is blocked  
- `CommunityScanResult.Status` is set to `BlockedBySafeLock`

### Operator Action Types

```csharp
public enum OperatorActionType
{
    ScanCommunity,          // Read-only scan of a community page
    ExtractCommunityContext // Extract structured context from scan data
}
```

---

## Build & Test

```bash
# From repository root
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Expected output: **All 40 tests pass.**

---

## Project Structure

```
meta7-sdk-csharp/
├── meta7-sdk-csharp.sln
├── src/
│   ├── META7.Operator.Api/          ← Operator execution layer
│   ├── META7.Operator.Contracts/    ← Shared contracts/DTOs
│   └── (META7.CaptainM7A SDK .cs files — Captain SDK, read-only)
└── tests/
    └── META7.Operator.Api.Tests/    ← Integration tests (40 tests)
```

---

## What This PR Does NOT Include

This PR intentionally excludes:
- Any write-capable browser actions (no posting, messaging, reacting)
- Any automation on social media or marketplaces
- Any login flows or credential storage
- Any Cloud Run or Dockerfile changes
- Any SAFE_LOCK modifications
- Any Captain SDK behaviour changes
- Any routing or canonical event store changes
- Any human-like typing or mouse simulation
- Any external API integration (Facebook API, LINE API, Discord API)

---

## What's Next

The next PR will introduce the **Autonomous Revenue Engine** — write-capable actions for community engagement, with full safety gate reviews, human-in-the-loop checkpoints, and audit logging.

---

## License

Proprietary — META7 Orchestration. All rights reserved.
