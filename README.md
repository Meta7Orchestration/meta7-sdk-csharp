# META7 SDK C#

## META7 Autonomous Outreach Engine (Read-Only)

This repository now includes a deterministic, controlled, read-only outreach module for the Embodied Operator flow.

### Architecture

- `OutreachSignalDetector` (read-only scanning and signal extraction)
- `OutreachContextAnalyzer` (signal-to-context conversion)
- `OutreachValuePropositionEngine` (deterministic structured value mapping)
- `OutreachSuggestionFormatter` (structured suggestion output)
- `DirectiveExecutionService` routing for outreach action types

### Read-Only Guarantees

- No post, message, submit, or write browser actions are used.
- Playwright integration is modeled strictly through read-only actions:
  - navigate
  - extract text
  - extract metadata
  - extract structure
- No social/marketplace automation or external API integrations are included.

### SAFE_LOCK and Policy Enforcement

- All outreach actions are blocked when `SAFE_LOCK` is active.
- Policy gate checks are enforced before each outreach action path.
- Domain allowlist enforcement is applied at detection time.

### Deterministic Test Coverage

`OperatorApiOutreachTests` covers:

- signal detection
- context analysis
- suggestion generation
- domain allowlist enforcement
- SAFE_LOCK blocking
- policy-gate blocking
- deterministic output checks

### Next PR

The next PR will introduce the Community Connector phase that includes controlled write actions and separate policy controls.
