# META7 Operator SDK (C#)

## META7 Autonomous Revenue Engine

This repository now includes a policy-gated Autonomous Revenue Engine that enables bounded write-capable execution for the Embodied Operator.

### Revenue architecture

- `RevenueOpportunityDetector` converts outreach context into a deterministic revenue opportunity.
- `RevenueFlowOrchestrator` combines outreach + community context into a deterministic `RevenueActionRequest`.
- `RevenueSafetyGate` applies strict policy checks (action type, allowlist, form structure, login/private-data rejection).
- `RevenueActionExecutor` executes only bounded write actions through `PlaywrightOperatorExecutor` abstractions.
- `DirectiveExecutionService` routes revenue write actions and enforces SAFE_LOCK + policy gate before execution.

### Bounded write actions

- `SubmitLeadForm`
- `RequestCallback`
- `TriggerWebhook`
- `CreateSupportTicket`
- `RegisterInterest`

### Safety guarantees

- SAFE_LOCK-aware write execution blocking.
- Strict domain allowlist enforcement.
- Deterministic request shaping and response behavior.
- Login-required and private-data actions are rejected.
- No social posting, messaging, marketplace mutation, or credential flows are enabled.

### Deterministic test coverage

`OperatorApiRevenueTests` validates:

- all bounded write action routes,
- allowlist enforcement,
- policy rejection behavior,
- SAFE_LOCK rejection,
- deterministic execution responses,
- orchestrator request determinism.

### Next step

The next PR (#9) will add the Human Approval Layer on top of this bounded execution model.
