# META7 Captain M7A SDK

> **จดจำไว้ แล้วไปด้วยกัน**

## Deterministic Cognitive Runtime for META7 / QHCU / CAIM

> **Intent becomes structure. Structure becomes governed execution. Execution becomes traceable evidence.**

---

## Table of Contents

- #the-signal
- #core-flow
- #quick-start
  - #requirements
  - #run-with-docker
  - #run-with-net
- #api-surface
- #gate-a-authorization
  - #governed-command-example
- #saga-execution
- #architecture
  - #trust-architecture
- #safety-invariants
- #deterministic-control-probabilistic-intelligence
- #met47-evidence-chain
- #replay-and-reconciliation
- #health-and-readiness
- #cloud-run-deployment
  - #deployment-principles
  - #suggested-environment-configuration
- #ci-quality-gates
- #repository-map
- #security
- #development-principles
- #roadmap
- #project-status
- #meta7-orbital-engine

---

## The Signal

**META7 Captain M7A SDK** is a C# runtime and orchestration framework for building:

- Governed cognitive services
- Deterministic command pipelines
- Saga workflows
- Replayable execution
- Cryptographically linked audit evidence

The project is designed around one uncompromising rule:

> **No command becomes an action until identity, authority, scope, state, and evidence are aligned.**

META7 does not treat AI output as authority.

- **CAIM** may interpret, advise, and propose.
- **M7A** governs execution.
- **SFL** constrains authorization.
- **MET47** preserves traceability.

Every layer has a defined role, a bounded scope, and an observable state transition.

---

## Core Flow

```text
Will-Source
    ↓
Declared Intent
    ↓
SFL Gate A Authorization
    ↓
Authorized Command Envelope
    ↓
M7A Command Pipeline
    ↓
Saga / Workflow / Cognitive Runtime
    ↓
Verification and Reconciliation
    ↓
MET47 Evidence Chain
    ↓
Will-Form
```

The runtime separates generative interpretation from deterministic control:

```text
CAIM Semantic Plane       proposes, interprets, recommends
META7 Control Plane       validates, routes, constrains
M7A Execution Plane       executes bounded commands
MET47 Evidence Plane      records, verifies, reconciles
```

---

## Quick Start

### Requirements

- .NET 10 SDK
- Docker, optional for container execution
- A terminal capable of running `dotnet`, `docker`, or `curl`

### Run with Docker

```bash
docker build -t meta7-sdk .

docker run --rm -p 8080:8080 \
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 \
  meta7-sdk
```

Open the API documentation:

```text
http://localhost:8080/swagger
```

### Run with .NET

Identify the deployable Web API project in the repository, then run:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet run --project <PATH_TO_WEB_API_PROJECT>
```

Replace `<PATH_TO_WEB_API_PROJECT>` with the actual executable project path in the repository.

---

## API Surface

| Method | Endpoint | Purpose | Expected exposure |
|---|---|---|---|
| `GET` | `/` | Runtime identity and system information | Public or restricted by environment |
| `GET` | `/meta7/captain/status` | Full Captain runtime status | Restricted |
| `GET` | `/meta7/captain/health` | Runtime health signal | Operational |
| `GET` | `/meta7/captain/layers` | Layer and capability information | Restricted |
| `POST` | `/meta7/captain/execute` | Execute a governed Captain command | Gate A required |
| `POST` | `/meta7/saga/run` | Start an approved Saga definition | Gate A required |
| `POST` | `/meta7/workflow/execute` | Execute an approved workflow revision | Gate A required |
| `GET` | `/swagger` | OpenAPI documentation | Development only by default |

> Production exposure is policy-controlled. The endpoint list does not grant anonymous execution rights.

---

## Gate A Authorization

All state-changing endpoints must fail closed.

> **Authentication alone is not authorization.**

A valid execution request is evaluated against:

```text
Identity
+ Capability
+ Action
+ Resource
+ Scope
+ Payload Binding
+ Freshness
+ Single-Use Nonce
+ Idempotency
+ Policy
= Authorization Decision
```

If any required condition is missing, invalid, expired, unavailable, or ambiguous, the decision is:

```text
DENY
```

### Governed Command Example

The following example uses placeholders.

> **Never commit real credentials, tokens, nonces, or private connection details.**

```bash
export META7_ACCESS_TOKEN="<short-lived-access-token>"
export META7_CAPABILITY_ID="<capability-id>"
export META7_NONCE="<single-use-nonce>"
export META7_IDEMPOTENCY_KEY="<unique-idempotency-key>"
```

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/captain/execute" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "layer": "core",
    "command": "ANALYZE",
    "payload": "Hello META7"
  }'
```

The server remains responsible for:

- Request normalization
- Capability verification
- Atomic nonce consumption
- Policy evaluation
- Audit recording

A digest supplied by a client is never treated as proof of authority by itself.

---

## Saga Execution

Saga execution uses server-approved workflow definitions and revisions.

Clients provide inputs, not unrestricted execution sequences.

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/saga/run" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "workflowId": "WF-001",
    "workflowRevision": "1",
    "input": {
      "target": "sandbox-host-01"
    },
    "executionPolicy": {
      "stopOnFailure": true
    }
  }'
```

A governed Saga may resolve to:

```text
CREATED
  ↓
AUTHORIZED
  ↓
PLANNED
  ↓
EXECUTING
  ├── SUCCEEDED
  ├── FAILED
  └── OUTCOME_UNKNOWN
          ↓
      RECONCILING
          ├── VERIFIED_SUCCESS
          ├── VERIFIED_FAILURE
          └── MANUAL_REVIEW
  ↓
EVIDENCE_COMMITTED
  ↓
CLOSED
```

> Timeout is not automatically classified as failure.

An unknown outcome enters reconciliation before a terminal decision is recorded.

---

## Architecture

```text
META7 Captain M7A SDK
│
├── Layers 01-05   Core Types and Strategic Intelligence
├── Layers 06-10   M7A Strategic Commander
├── Layers 11-15   Strategic Cognitive Loop
├── Layers 16-20   Command Pipeline and Gateway
├── Layers 21-25   Barrier Network MB-SYNC-001
├── Layers 26-30   Audit Ledger and Replay Engine
│
├── M7A Saga Orchestration Engine
├── Workflow Engine V3
├── SFL Gate A Policy Boundary
├── QHCU Temporal State Model
├── CAIM Advisory Plane
└── MET47 Evidence and Reconciliation Plane
```

### Trust Architecture

```text
UNTRUSTED DOMAIN
├── Browser and external clients
├── AI-generated proposals
├── External model providers
└── Third-party service callbacks
          │
          ▼
SFL GATE A
├── Identity verification
├── Capability and scope validation
├── Payload and resource binding
├── Nonce and replay control
└── Default DENY policy
          │
          ▼
M7A ENFORCEMENT PLANE
├── Authorized Command Envelope
├── Captain Runtime
├── Saga Engine
└── Workflow Engine
          │
          ▼
MET47 CONTROL AND EVIDENCE PLANE
├── State transition records
├── Reconciliation events
├── Replay verification
└── Chain-linked audit evidence
```

No component is trusted because of its name or network location.

Every component receives explicit, minimal authority and remains independently auditable.

---

## Safety Invariants

The following invariants define the intended safety contract of the runtime:

| Invariant | Required guarantee |
|---|---|
| `ProjectionAtomicity` | A projection is committed completely or not committed |
| `SequenceConsistency` | State transitions follow approved ordering rules |
| `EpochConsistency` | Temporal operations remain bound to the correct epoch |
| `HashChainIntegrity` | Evidence records preserve valid chain linkage |
| `ReplayEquivalence` | Authorized replay produces policy-equivalent behavior |
| `BarrierConsistency` | Barrier decisions remain coherent across participating layers |
| `ActionSafety` | No action exceeds its declared authority or safety boundary |

Invariant status must be evidence-based:

```text
DECLARED
  ↓
IMPLEMENTED
  ↓
TESTED
  ↓
RUNTIME_VERIFIED
```

Do not label an invariant `ENFORCED` unless all of the following are available:

1. An enforcement point
2. Defined failure behavior
3. An automated test
4. Runtime evidence

---

## Deterministic Control, Probabilistic Intelligence

META7 does not claim that a generative model produces identical language on every call.

Determinism applies to the control plane:

- Input and contract validation
- Policy routing
- State-transition eligibility
- Idempotency behavior
- Replay and nonce decisions
- Evidence classification
- Terminal-state exclusivity
- Audit and reconciliation rules

Generative output remains advisory until it passes the declared validation and authority boundaries.

```text
Coherence            != Truth
Interpretation       != Validation
Model consensus      != Independent evidence
Generated proposal   != Authorized action
Timeout              != Confirmed failure
Hash linkage         != Absolute immutability
```

---

## MET47 Evidence Chain

MET47 records are designed to be tamper-evident through:

- Canonical serialization
- SHA-256 hashing
- Linkage to the previous record

A typical evidence record may include:

```json
{
  "eventType": "COMMAND_EXECUTED",
  "commandId": "cmd-001",
  "authorizationDecisionId": "authz-001",
  "principalReference": "principal-ref",
  "workflowId": "WF-001",
  "workflowRevision": "1",
  "outcome": "SUCCEEDED",
  "policyVersion": "sfl-policy-001",
  "timestamp": "<utc-timestamp>",
  "previousHash": "<previous-record-hash>",
  "hash": "<current-record-hash>"
}
```

Hash chaining provides tamper evidence.

Stronger immutability requires one or more of the following:

- Protected writers
- Append-only storage
- Trusted checkpoints
- Independent verification
- External anchoring

---

## Replay and Reconciliation

Replay is not blind repetition.

A replay operation must preserve:

- Source command identity
- Workflow and policy revision
- Temporal context
- Capability boundaries
- Idempotency semantics
- Evidence lineage
- Declared side-effect policy

Unknown or partially observed outcomes enter reconciliation:

```text
OUTCOME_UNKNOWN
     ↓
Inspect durable state
     ↓
Query external operation reference
     ↓
Compare command, evidence, and side effects
     ├── Confirm success
     ├── Confirm failure
     └── Escalate for manual review
```

---

## Health and Readiness

The existing Captain health endpoint is:

```http
GET /meta7/captain/health
```

For production deployments, separate probes are recommended:

```http
GET /health/live
GET /health/ready
```

### Liveness

`live` should report process viability.

### Readiness

`ready` should report whether the following components are available for safe command execution:

- Policy
- State
- Nonce
- Evidence
- Required dependencies

> If a critical dependency cannot be verified, state-changing operations must fail closed.

---

## Cloud Run Deployment

The API is intended to run as an immutable container revision.

```text
GitHub repository
      ↓
Build, test, and security validation
      ↓
Immutable container image
      ↓
Artifact Registry
      ↓
Cloud Run staging
      ↓
Health, readiness, and Gate A validation
      ↓
Protected production approval
      ↓
Cloud Run production revision
      ↓
MET47 deployment evidence
```

### Deployment Principles

- Use GitHub OpenID Connect and Google Workload Identity Federation
- Avoid long-lived cloud service-account keys
- Separate deployment identity from runtime identity
- Deploy exact commit SHAs and image digests
- Validate staging before production promotion
- Preserve the previous verified revision for rollback
- Protect the production GitHub Environment with required approval
- Store runtime secrets in an approved secret-management system
- Never publish credentials or production connection details in documentation

### Suggested Environment Configuration

```text
GCP_PROJECT_ID
GCP_REGION
CLOUD_RUN_SERVICE
ARTIFACT_REGISTRY
WORKLOAD_IDENTITY_PROVIDER
DEPLOYER_SERVICE_ACCOUNT
RUNTIME_SERVICE_ACCOUNT
```

These are identifiers and configuration names.

> Secret values must remain outside the repository.

---

## CI Quality Gates

A production candidate should pass the following sequence:

```text
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Invariant Tests
  ↓
Authorization Negative Tests
  ↓
Dependency and Container Scan
  ↓
Artifact Provenance
  ↓
Staging Deployment
  ↓
Runtime Smoke Test
  ↓
Replay and Reconciliation Test
  ↓
Production Approval
```

Recommended commands:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

> A successful build proves compilation. It does not by itself prove authorization integrity, runtime safety, ledger integrity, or production readiness.

---

## Repository Map

```text
.
├── .github/          GitHub configuration and automation
├── META7.SDK/        META7 SDK components
├── src/              Runtime and source projects
├── README.md         Project entry point
├── SECURITY.md       Security policy and reporting process
├── AcrobaticDemo.cs  Demonstration source
└── setup-github.sh   Repository setup helper
```

The exact project boundaries may evolve.

Runtime, SDK, contracts, tests, and deployment infrastructure should remain separable even when hosted in the same repository.

---

## Security

Before reporting a vulnerability, read [`SECURITY.md`](SECURITY.md).

Do not include the following in public issues, discussions, pull requests, logs, screenshots, or documentation:

- Passwords
- Access tokens
- Private keys
- Service-account keys
- Real nonces or capability secrets
- Payment credentials
- Private infrastructure endpoints
- Sensitive customer or operational data

Security-sensitive execution paths should include:

- Negative tests
- Concurrency tests
- Crash-recovery tests
- Audit verification

---

## Development Principles

Contributions should preserve the following rules.

### 1. Intent before execution

Every command must identify its purpose and authority.

### 2. Default DENY

Missing evidence never becomes implicit permission.

### 3. Immutable source identity

A revision may supersede an earlier intent, but history is not rewritten.

### 4. Explicit state transitions

No hidden transition and no silent terminal state.

### 5. Idempotent side effects

Retries must not duplicate business effects.

### 6. Traceable transformation

Every material output must point back to its source, field, policy, and evidence.

### 7. Governed intelligence

AI may recommend. Authority must authorize.

---

## Roadmap

- [ ] Publish a stable API and compatibility policy
- [ ] Separate liveness and readiness endpoints
- [ ] Document the Gate A capability schema
- [ ] Publish command-envelope and evidence-envelope specifications
- [ ] Add invariant-focused test reports
- [ ] Add replay and reconciliation test fixtures
- [ ] Add container provenance and SBOM generation
- [ ] Add staging-to-production promotion controls
- [ ] Add signed MET47 checkpoints
- [ ] Publish versioned NuGet packages when the public SDK contract is stable

---

## Project Status

META7 Captain M7A SDK is an evolving architecture.

Endpoint availability, invariant maturity, and deployment readiness must be verified against:

- Current source
- Automated tests
- Runtime evidence

```text
Build passed           means compilation succeeded
Tests passed           means declared tests succeeded
Runtime verified       means controlled execution was observed
Security validated     means defined adversarial gates passed
Production authorized  means the designated authority approved release
```

---

## META7 Orbital Engine

> **Remember the intent. Govern the field. Verify the form.**

```text
Will-Source → Will-Field → Coherence → Will-Form → Hyper-Vector
```

---

**HopeCpluS Foundation**
|---|---|---|---|
| `GET` | `/` | Runtime identity and system information | Public or restricted by environment |
| `GET` | `/meta7/captain/status` | Full Captain runtime status | Restricted |
| `GET` | `/meta7/captain/health` | Runtime health signal | Operational |
| `GET` | `/meta7/captain/layers` | Layer and capability information | Restricted |
| `POST` | `/meta7/captain/execute` | Execute a governed Captain command | Gate A required |
| `POST` | `/meta7/saga/run` | Start an approved Saga definition | Gate A required |
| `POST` | `/meta7/workflow/execute` | Execute an approved workflow revision | Gate A required |
| `GET` | `/swagger` | OpenAPI documentation | Development only by default |

> Production exposure is policy-controlled. The endpoint list does not grant anonymous execution rights.

---

## Gate A Authorization

All state-changing endpoints must fail closed.

> **Authentication alone is not authorization.**

A valid execution request is evaluated against:

```text
Identity
+ Capability
+ Action
+ Resource
+ Scope
+ Payload Binding
+ Freshness
+ Single-Use Nonce
+ Idempotency
+ Policy
= Authorization Decision
```

If any required condition is missing, invalid, expired, unavailable, or ambiguous, the decision is:

```text
DENY
```

### Governed Command Example

The following example uses placeholders.

> **Never commit real credentials, tokens, nonces, or private connection details.**

```bash
export META7_ACCESS_TOKEN="<short-lived-access-token>"
export META7_CAPABILITY_ID="<capability-id>"
export META7_NONCE="<single-use-nonce>"
export META7_IDEMPOTENCY_KEY="<unique-idempotency-key>"
```

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/captain/execute" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "layer": "core",
    "command": "ANALYZE",
    "payload": "Hello META7"
  }'
```

The server remains responsible for:

- Request normalization
- Capability verification
- Atomic nonce consumption
- Policy evaluation
- Audit recording

A digest supplied by a client is never treated as proof of authority by itself.

---

## Saga Execution

Saga execution uses server-approved workflow definitions and revisions.

Clients provide inputs, not unrestricted execution sequences.

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/saga/run" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "workflowId": "WF-001",
    "workflowRevision": "1",
    "input": {
      "target": "sandbox-host-01"
    },
    "executionPolicy": {
      "stopOnFailure": true
    }
  }'
```

A governed Saga may resolve to:

```text
CREATED
  ↓
AUTHORIZED
  ↓
PLANNED
  ↓
EXECUTING
  ├── SUCCEEDED
  ├── FAILED
  └── OUTCOME_UNKNOWN
          ↓
      RECONCILING
          ├── VERIFIED_SUCCESS
          ├── VERIFIED_FAILURE
          └── MANUAL_REVIEW
  ↓
EVIDENCE_COMMITTED
  ↓
CLOSED
```

> Timeout is not automatically classified as failure.

An unknown outcome enters reconciliation before a terminal decision is recorded.

---

## Architecture

```text
META7 Captain M7A SDK
│
├── Layers 01-05   Core Types and Strategic Intelligence
├── Layers 06-10   M7A Strategic Commander
├── Layers 11-15   Strategic Cognitive Loop
├── Layers 16-20   Command Pipeline and Gateway
├── Layers 21-25   Barrier Network MB-SYNC-001
├── Layers 26-30   Audit Ledger and Replay Engine
│
├── M7A Saga Orchestration Engine
├── Workflow Engine V3
├── SFL Gate A Policy Boundary
├── QHCU Temporal State Model
├── CAIM Advisory Plane
└── MET47 Evidence and Reconciliation Plane
```

### Trust Architecture

```text
UNTRUSTED DOMAIN
├── Browser and external clients
├── AI-generated proposals
├── External model providers
└── Third-party service callbacks
          │
          ▼
SFL GATE A
├── Identity verification
├── Capability and scope validation
├── Payload and resource binding
├── Nonce and replay control
└── Default DENY policy
          │
          ▼
M7A ENFORCEMENT PLANE
├── Authorized Command Envelope
├── Captain Runtime
├── Saga Engine
└── Workflow Engine
          │
          ▼
MET47 CONTROL AND EVIDENCE PLANE
├── State transition records
├── Reconciliation events
├── Replay verification
└── Chain-linked audit evidence
```

No component is trusted because of its name or network location.

Every component receives explicit, minimal authority and remains independently auditable.

---

## Safety Invariants

The following invariants define the intended safety contract of the runtime:

| Invariant | Required guarantee |
|---|---|
| `ProjectionAtomicity` | A projection is committed completely or not committed |
| `SequenceConsistency` | State transitions follow approved ordering rules |
| `EpochConsistency` | Temporal operations remain bound to the correct epoch |
| `HashChainIntegrity` | Evidence records preserve valid chain linkage |
| `ReplayEquivalence` | Authorized replay produces policy-equivalent behavior |
| `BarrierConsistency` | Barrier decisions remain coherent across participating layers |
| `ActionSafety` | No action exceeds its declared authority or safety boundary |

Invariant status must be evidence-based:

```text
DECLARED
  ↓
IMPLEMENTED
  ↓
TESTED
  ↓
RUNTIME_VERIFIED
```

Do not label an invariant `ENFORCED` unless all of the following are available:

1. An enforcement point
2. Defined failure behavior
3. An automated test
4. Runtime evidence

---

## Deterministic Control, Probabilistic Intelligence

META7 does not claim that a generative model produces identical language on every call.

Determinism applies to the control plane:

- Input and contract validation
- Policy routing
- State-transition eligibility
- Idempotency behavior
- Replay and nonce decisions
- Evidence classification
- Terminal-state exclusivity
- Audit and reconciliation rules

Generative output remains advisory until it passes the declared validation and authority boundaries.

```text
Coherence            != Truth
Interpretation       != Validation
Model consensus      != Independent evidence
Generated proposal   != Authorized action
Timeout              != Confirmed failure
Hash linkage         != Absolute immutability
```

---

## MET47 Evidence Chain

MET47 records are designed to be tamper-evident through:

- Canonical serialization
- SHA-256 hashing
- Linkage to the previous record

A typical evidence record may include:

```json
{
  "eventType": "COMMAND_EXECUTED",
  "commandId": "cmd-001",
  "authorizationDecisionId": "authz-001",
  "principalReference": "principal-ref",
  "workflowId": "WF-001",
  "workflowRevision": "1",
  "outcome": "SUCCEEDED",
  "policyVersion": "sfl-policy-001",
  "timestamp": "<utc-timestamp>",
  "previousHash": "<previous-record-hash>",
  "hash": "<current-record-hash>"
}
```

Hash chaining provides tamper evidence.

Stronger immutability requires one or more of the following:

- Protected writers
- Append-only storage
- Trusted checkpoints
- Independent verification
- External anchoring

---

## Replay and Reconciliation

Replay is not blind repetition.

A replay operation must preserve:

- Source command identity
- Workflow and policy revision
- Temporal context
- Capability boundaries
- Idempotency semantics
- Evidence lineage
- Declared side-effect policy

Unknown or partially observed outcomes enter reconciliation:

```text
OUTCOME_UNKNOWN
     ↓
Inspect durable state
     ↓
Query external operation reference
     ↓
Compare command, evidence, and side effects
     ├── Confirm success
     ├── Confirm failure
     └── Escalate for manual review
```

---

## Health and Readiness

The existing Captain health endpoint is:

```http
GET /meta7/captain/health
```

For production deployments, separate probes are recommended:

```http
GET /health/live
GET /health/ready
```

### Liveness

`live` should report process viability.

### Readiness

`ready` should report whether the following components are available for safe command execution:

- Policy
- State
- Nonce
- Evidence
- Required dependencies

> If a critical dependency cannot be verified, state-changing operations must fail closed.

---

## Cloud Run Deployment

The API is intended to run as an immutable container revision.

```text
GitHub repository
      ↓
Build, test, and security validation
      ↓
Immutable container image
      ↓
Artifact Registry
      ↓
Cloud Run staging
      ↓
Health, readiness, and Gate A validation
      ↓
Protected production approval
      ↓
Cloud Run production revision
      ↓
MET47 deployment evidence
```

### Deployment Principles

- Use GitHub OpenID Connect and Google Workload Identity Federation
- Avoid long-lived cloud service-account keys
- Separate deployment identity from runtime identity
- Deploy exact commit SHAs and image digests
- Validate staging before production promotion
- Preserve the previous verified revision for rollback
- Protect the production GitHub Environment with required approval
- Store runtime secrets in an approved secret-management system
- Never publish credentials or production connection details in documentation

### Suggested Environment Configuration

```text
GCP_PROJECT_ID
GCP_REGION
CLOUD_RUN_SERVICE
ARTIFACT_REGISTRY
WORKLOAD_IDENTITY_PROVIDER
DEPLOYER_SERVICE_ACCOUNT
RUNTIME_SERVICE_ACCOUNT
```

These are identifiers and configuration names.

> Secret values must remain outside the repository.

---

## CI Quality Gates

A production candidate should pass the following sequence:

```text
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Invariant Tests
  ↓
Authorization Negative Tests
  ↓
Dependency and Container Scan
  ↓
Artifact Provenance
  ↓
Staging Deployment
  ↓
Runtime Smoke Test
  ↓
Replay and Reconciliation Test
  ↓
Production Approval
```

Recommended commands:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

> A successful build proves compilation. It does not by itself prove authorization integrity, runtime safety, ledger integrity, or production readiness.

---

## Repository Map

```text
.
├── .github/          GitHub configuration and automation
├── META7.SDK/        META7 SDK components
├── src/              Runtime and source projects
├── README.md         Project entry point
├── SECURITY.md       Security policy and reporting process
├── AcrobaticDemo.cs  Demonstration source
└── setup-github.sh   Repository setup helper
```

The exact project boundaries may evolve.

Runtime, SDK, contracts, tests, and deployment infrastructure should remain separable even when hosted in the same repository.

---

## Security

Before reporting a vulnerability, read [`SECURITY.md`](SECURITY.md).

Do not include the following in public issues, discussions, pull requests, logs, screenshots, or documentation:

- Passwords
- Access tokens
- Private keys
- Service-account keys
- Real nonces or capability secrets
- Payment credentials
- Private infrastructure endpoints
- Sensitive customer or operational data

Security-sensitive execution paths should include:

- Negative tests
- Concurrency tests
- Crash-recovery tests
- Audit verification

---

## Development Principles

Contributions should preserve the following rules.

### 1. Intent before execution

Every command must identify its purpose and authority.

### 2. Default DENY

Missing evidence never becomes implicit permission.

### 3. Immutable source identity

A revision may supersede an earlier intent, but history is not rewritten.

### 4. Explicit state transitions

No hidden transition and no silent terminal state.

### 5. Idempotent side effects

Retries must not duplicate business effects.

### 6. Traceable transformation

Every material output must point back to its source, field, policy, and evidence.

### 7. Governed intelligence

AI may recommend. Authority must authorize.

---

## Roadmap

- [ ] Publish a stable API and compatibility policy
- [ ] Separate liveness and readiness endpoints
- [ ] Document the Gate A capability schema
- [ ] Publish command-envelope and evidence-envelope specifications
- [ ] Add invariant-focused test reports
- [ ] Add replay and reconciliation test fixtures
- [ ] Add container provenance and SBOM generation
- [ ] Add staging-to-production promotion controls
- [ ] Add signed MET47 checkpoints
- [ ] Publish versioned NuGet packages when the public SDK contract is stable

---

## Project Status

META7 Captain M7A SDK is an evolving architecture.

Endpoint availability, invariant maturity, and deployment readiness must be verified against:

- Current source
- Automated tests
- Runtime evidence

```text
Build passed           means compilation succeeded
Tests passed           means declared tests succeeded
Runtime verified       means controlled execution was observed
Security validated     means defined adversarial gates passed
Production authorized  means the designated authority approved release
```

---

## META7 Orbital Engine

> **Remember the intent. Govern the field. Verify the form.**

```text
Will-Source → Will-Field → Coherence → Will-Form → Hyper-Vector
```

---

**HopeCpluS Foundation**
# META7 Captain M7A SDK

> **จดจำไว้ แล้วไปด้วยกัน**

## Deterministic Cognitive Runtime for META7 / QHCU / CAIM

> **Intent becomes structure. Structure becomes governed execution. Execution becomes traceable evidence.**

---

## Table of Contents

- #the-signal
- #core-flow
- #quick-start
  - #requirements
  - #run-with-docker
  - #run-with-net
- #api-surface
- #gate-a-authorization
  - #governed-command-example
- #saga-execution
- #architecture
  - #trust-architecture
- #safety-invariants
- #deterministic-control-probabilistic-intelligence
- #met47-evidence-chain
- #replay-and-reconciliation
- #health-and-readiness
- #cloud-run-deployment
  - #deployment-principles
  - #suggested-environment-configuration
- #ci-quality-gates
- #repository-map
- #security
- #development-principles
- #roadmap
- #project-status
- #meta7-orbital-engine

---

## The Signal

**META7 Captain M7A SDK** is a C# runtime and orchestration framework for building:

- Governed cognitive services
- Deterministic command pipelines
- Saga workflows
- Replayable execution
- Cryptographically linked audit evidence

The project is designed around one uncompromising rule:

> **No command becomes an action until identity, authority, scope, state, and evidence are aligned.**

META7 does not treat AI output as authority.

- **CAIM** may interpret, advise, and propose.
- **M7A** governs execution.
- **SFL** constrains authorization.
- **MET47** preserves traceability.

Every layer has a defined role, a bounded scope, and an observable state transition.

---

## Core Flow

```text
Will-Source
    ↓
Declared Intent
    ↓
SFL Gate A Authorization
    ↓
Authorized Command Envelope
    ↓
M7A Command Pipeline
    ↓
Saga / Workflow / Cognitive Runtime
    ↓
Verification and Reconciliation
    ↓
MET47 Evidence Chain
    ↓
Will-Form
```

The runtime separates generative interpretation from deterministic control:

```text
CAIM Semantic Plane       proposes, interprets, recommends
META7 Control Plane       validates, routes, constrains
M7A Execution Plane       executes bounded commands
MET47 Evidence Plane      records, verifies, reconciles
```

---

## Quick Start

### Requirements

- .NET 10 SDK
- Docker, optional for container execution
- A terminal capable of running `dotnet`, `docker`, or `curl`

### Run with Docker

```bash
docker build -t meta7-sdk .

docker run --rm -p 8080:8080 \
  -e ASPNETCORE_URLS=http://0.0.0.0:8080 \
  meta7-sdk
```

Open the API documentation:

```text
http://localhost:8080/swagger
```

### Run with .NET

Identify the deployable Web API project in the repository, then run:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet run --project <PATH_TO_WEB_API_PROJECT>
```

Replace `<PATH_TO_WEB_API_PROJECT>` with the actual executable project path in the repository.

---

## API Surface

| Method | Endpoint | Purpose | Expected exposure |
|---|---|---|---|
| `GET` | `/` | Runtime identity and system information | Public or restricted by environment |
| `GET` | `/meta7/captain/status` | Full Captain runtime status | Restricted |
| `GET` | `/meta7/captain/health` | Runtime health signal | Operational |
| `GET` | `/meta7/captain/layers` | Layer and capability information | Restricted |
| `POST` | `/meta7/captain/execute` | Execute a governed Captain command | Gate A required |
| `POST` | `/meta7/saga/run` | Start an approved Saga definition | Gate A required |
| `POST` | `/meta7/workflow/execute` | Execute an approved workflow revision | Gate A required |
| `GET` | `/swagger` | OpenAPI documentation | Development only by default |

> Production exposure is policy-controlled. The endpoint list does not grant anonymous execution rights.

---

## Gate A Authorization

All state-changing endpoints must fail closed.

> **Authentication alone is not authorization.**

A valid execution request is evaluated against:

```text
Identity
+ Capability
+ Action
+ Resource
+ Scope
+ Payload Binding
+ Freshness
+ Single-Use Nonce
+ Idempotency
+ Policy
= Authorization Decision
```

If any required condition is missing, invalid, expired, unavailable, or ambiguous, the decision is:

```text
DENY
```

### Governed Command Example

The following example uses placeholders.

> **Never commit real credentials, tokens, nonces, or private connection details.**

```bash
export META7_ACCESS_TOKEN="<short-lived-access-token>"
export META7_CAPABILITY_ID="<capability-id>"
export META7_NONCE="<single-use-nonce>"
export META7_IDEMPOTENCY_KEY="<unique-idempotency-key>"
```

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/captain/execute" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "layer": "core",
    "command": "ANALYZE",
    "payload": "Hello META7"
  }'
```

The server remains responsible for:

- Request normalization
- Capability verification
- Atomic nonce consumption
- Policy evaluation
- Audit recording

A digest supplied by a client is never treated as proof of authority by itself.

---

## Saga Execution

Saga execution uses server-approved workflow definitions and revisions.

Clients provide inputs, not unrestricted execution sequences.

```bash
curl -X POST "https://meta7.hopecplus.com/meta7/saga/run" \
  -H "Authorization: Bearer ${META7_ACCESS_TOKEN}" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: ${META7_IDEMPOTENCY_KEY}" \
  -H "X-META7-Capability: ${META7_CAPABILITY_ID}" \
  -H "X-META7-Nonce: ${META7_NONCE}" \
  -d '{
    "workflowId": "WF-001",
    "workflowRevision": "1",
    "input": {
      "target": "sandbox-host-01"
    },
    "executionPolicy": {
      "stopOnFailure": true
    }
  }'
```

A governed Saga may resolve to:

```text
CREATED
  ↓
AUTHORIZED
  ↓
PLANNED
  ↓
EXECUTING
  ├── SUCCEEDED
  ├── FAILED
  └── OUTCOME_UNKNOWN
          ↓
      RECONCILING
          ├── VERIFIED_SUCCESS
          ├── VERIFIED_FAILURE
          └── MANUAL_REVIEW
  ↓
EVIDENCE_COMMITTED
  ↓
CLOSED
```

> Timeout is not automatically classified as failure.

An unknown outcome enters reconciliation before a terminal decision is recorded.

---

## Architecture

```text
META7 Captain M7A SDK
│
├── Layers 01-05   Core Types and Strategic Intelligence
├── Layers 06-10   M7A Strategic Commander
├── Layers 11-15   Strategic Cognitive Loop
├── Layers 16-20   Command Pipeline and Gateway
├── Layers 21-25   Barrier Network MB-SYNC-001
├── Layers 26-30   Audit Ledger and Replay Engine
│
├── M7A Saga Orchestration Engine
├── Workflow Engine V3
├── SFL Gate A Policy Boundary
├── QHCU Temporal State Model
├── CAIM Advisory Plane
└── MET47 Evidence and Reconciliation Plane
```

### Trust Architecture

```text
UNTRUSTED DOMAIN
├── Browser and external clients
├── AI-generated proposals
├── External model providers
└── Third-party service callbacks
          │
          ▼
SFL GATE A
├── Identity verification
├── Capability and scope validation
├── Payload and resource binding
├── Nonce and replay control
└── Default DENY policy
          │
          ▼
M7A ENFORCEMENT PLANE
├── Authorized Command Envelope
├── Captain Runtime
├── Saga Engine
└── Workflow Engine
          │
          ▼
MET47 CONTROL AND EVIDENCE PLANE
├── State transition records
├── Reconciliation events
├── Replay verification
└── Chain-linked audit evidence
```

No component is trusted because of its name or network location.

Every component receives explicit, minimal authority and remains independently auditable.

---

## Safety Invariants

The following invariants define the intended safety contract of the runtime:

| Invariant | Required guarantee |
|---|---|
| `ProjectionAtomicity` | A projection is committed completely or not committed |
| `SequenceConsistency` | State transitions follow approved ordering rules |
| `EpochConsistency` | Temporal operations remain bound to the correct epoch |
| `HashChainIntegrity` | Evidence records preserve valid chain linkage |
| `ReplayEquivalence` | Authorized replay produces policy-equivalent behavior |
| `BarrierConsistency` | Barrier decisions remain coherent across participating layers |
| `ActionSafety` | No action exceeds its declared authority or safety boundary |

Invariant status must be evidence-based:

```text
DECLARED
  ↓
IMPLEMENTED
  ↓
TESTED
  ↓
RUNTIME_VERIFIED
```

Do not label an invariant `ENFORCED` unless all of the following are available:

1. An enforcement point
2. Defined failure behavior
3. An automated test
4. Runtime evidence

---

## Deterministic Control, Probabilistic Intelligence

META7 does not claim that a generative model produces identical language on every call.

Determinism applies to the control plane:

- Input and contract validation
- Policy routing
- State-transition eligibility
- Idempotency behavior
- Replay and nonce decisions
- Evidence classification
- Terminal-state exclusivity
- Audit and reconciliation rules

Generative output remains advisory until it passes the declared validation and authority boundaries.

```text
Coherence            != Truth
Interpretation       != Validation
Model consensus      != Independent evidence
Generated proposal   != Authorized action
Timeout              != Confirmed failure
Hash linkage         != Absolute immutability
```

---

## MET47 Evidence Chain

MET47 records are designed to be tamper-evident through:

- Canonical serialization
- SHA-256 hashing
- Linkage to the previous record

A typical evidence record may include:

```json
{
  "eventType": "COMMAND_EXECUTED",
  "commandId": "cmd-001",
  "authorizationDecisionId": "authz-001",
  "principalReference": "principal-ref",
  "workflowId": "WF-001",
  "workflowRevision": "1",
  "outcome": "SUCCEEDED",
  "policyVersion": "sfl-policy-001",
  "timestamp": "<utc-timestamp>",
  "previousHash": "<previous-record-hash>",
  "hash": "<current-record-hash>"
}
```

Hash chaining provides tamper evidence.

Stronger immutability requires one or more of the following:

- Protected writers
- Append-only storage
- Trusted checkpoints
- Independent verification
- External anchoring

---

## Replay and Reconciliation

Replay is not blind repetition.

A replay operation must preserve:

- Source command identity
- Workflow and policy revision
- Temporal context
- Capability boundaries
- Idempotency semantics
- Evidence lineage
- Declared side-effect policy

Unknown or partially observed outcomes enter reconciliation:

```text
OUTCOME_UNKNOWN
     ↓
Inspect durable state
     ↓
Query external operation reference
     ↓
Compare command, evidence, and side effects
     ├── Confirm success
     ├── Confirm failure
     └── Escalate for manual review
```

---

## Health and Readiness

The existing Captain health endpoint is:

```http
GET /meta7/captain/health
```

For production deployments, separate probes are recommended:

```http
GET /health/live
GET /health/ready
```

### Liveness

`live` should report process viability.

### Readiness

`ready` should report whether the following components are available for safe command execution:

- Policy
- State
- Nonce
- Evidence
- Required dependencies

> If a critical dependency cannot be verified, state-changing operations must fail closed.

---

## Cloud Run Deployment

The API is intended to run as an immutable container revision.

```text
GitHub repository
      ↓
Build, test, and security validation
      ↓
Immutable container image
      ↓
Artifact Registry
      ↓
Cloud Run staging
      ↓
Health, readiness, and Gate A validation
      ↓
Protected production approval
      ↓
Cloud Run production revision
      ↓
MET47 deployment evidence
```

### Deployment Principles

- Use GitHub OpenID Connect and Google Workload Identity Federation
- Avoid long-lived cloud service-account keys
- Separate deployment identity from runtime identity
- Deploy exact commit SHAs and image digests
- Validate staging before production promotion
- Preserve the previous verified revision for rollback
- Protect the production GitHub Environment with required approval
- Store runtime secrets in an approved secret-management system
- Never publish credentials or production connection details in documentation

### Suggested Environment Configuration

```text
GCP_PROJECT_ID
GCP_REGION
CLOUD_RUN_SERVICE
ARTIFACT_REGISTRY
WORKLOAD_IDENTITY_PROVIDER
DEPLOYER_SERVICE_ACCOUNT
RUNTIME_SERVICE_ACCOUNT
```

These are identifiers and configuration names.

> Secret values must remain outside the repository.

---

## CI Quality Gates

A production candidate should pass the following sequence:

```text
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Invariant Tests
  ↓
Authorization Negative Tests
  ↓
Dependency and Container Scan
  ↓
Artifact Provenance
  ↓
Staging Deployment
  ↓
Runtime Smoke Test
  ↓
Replay and Reconciliation Test
  ↓
Production Approval
```

Recommended commands:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

> A successful build proves compilation. It does not by itself prove authorization integrity, runtime safety, ledger integrity, or production readiness.

---

## Repository Map

```text
.
├── .github/          GitHub configuration and automation
├── META7.SDK/        META7 SDK components
├── src/              Runtime and source projects
├── README.md         Project entry point
├── SECURITY.md       Security policy and reporting process
├── AcrobaticDemo.cs  Demonstration source
└── setup-github.sh   Repository setup helper
```

The exact project boundaries may evolve.

Runtime, SDK, contracts, tests, and deployment infrastructure should remain separable even when hosted in the same repository.

---

## Security

Before reporting a vulnerability, read [`SECURITY.md`](SECURITY.md).

Do not include the following in public issues, discussions, pull requests, logs, screenshots, or documentation:

- Passwords
- Access tokens
- Private keys
- Service-account keys
- Real nonces or capability secrets
- Payment credentials
- Private infrastructure endpoints
- Sensitive customer or operational data

Security-sensitive execution paths should include:

- Negative tests
- Concurrency tests
- Crash-recovery tests
- Audit verification

---

## Development Principles

Contributions should preserve the following rules.

### 1. Intent before execution

Every command must identify its purpose and authority.

### 2. Default DENY

Missing evidence never becomes implicit permission.

### 3. Immutable source identity

A revision may supersede an earlier intent, but history is not rewritten.

### 4. Explicit state transitions

No hidden transition and no silent terminal state.

### 5. Idempotent side effects

Retries must not duplicate business effects.

### 6. Traceable transformation

Every material output must point back to its source, field, policy, and evidence.

### 7. Governed intelligence

AI may recommend. Authority must authorize.

---

## Roadmap

- [ ] Publish a stable API and compatibility policy
- [ ] Separate liveness and readiness endpoints
- [ ] Document the Gate A capability schema
- [ ] Publish command-envelope and evidence-envelope specifications
- [ ] Add invariant-focused test reports
- [ ] Add replay and reconciliation test fixtures
- [ ] Add container provenance and SBOM generation
- [ ] Add staging-to-production promotion controls
- [ ] Add signed MET47 checkpoints
- [ ] Publish versioned NuGet packages when the public SDK contract is stable

---

## Project Status

META7 Captain M7A SDK is an evolving architecture.

Endpoint availability, invariant maturity, and deployment readiness must be verified against:

- Current source
- Automated tests
- Runtime evidence

```text
Build passed           means compilation succeeded
Tests passed           means declared tests succeeded
Runtime verified       means controlled execution was observed
Security validated     means defined adversarial gates passed
Production authorized  means the designated authority approved release
```

---

## META7 Orbital Engine

> **Remember the intent. Govern the field. Verify the form.**

```text
Will-Source → Will-Field → Coherence → Will-Form → Hyper-Vector
```

---

**HopeCpluS Foundation**
    },
    "executionPolicy": {
      "stopOnFailure": true
    }
  }'
A governed Saga may resolve to:
CREATED
  ↓
AUTHORIZED
  ↓
PLANNED
  ↓
EXECUTING
  ├── SUCCEEDED
  ├── FAILED
  └── OUTCOME_UNKNOWN
          ↓
      RECONCILING
          ├── VERIFIED_SUCCESS
          ├── VERIFIED_FAILURE
          └── MANUAL_REVIEW
  ↓
EVIDENCE_COMMITTED
  ↓
CLOSED
Timeout is not automatically classified as failure. An unknown outcome enters reconciliation before a terminal decision is recorded.
 
Architecture
META7 Captain M7A SDK
│
├── Layers 01-05   Core Types and Strategic Intelligence
├── Layers 06-10   M7A Strategic Commander
├── Layers 11-15   Strategic Cognitive Loop
├── Layers 16-20   Command Pipeline and Gateway
├── Layers 21-25   Barrier Network MB-SYNC-001
├── Layers 26-30   Audit Ledger and Replay Engine
│
├── M7A Saga Orchestration Engine
├── Workflow Engine V3
├── SFL Gate A Policy Boundary
├── QHCU Temporal State Model
├── CAIM Advisory Plane
└── MET47 Evidence and Reconciliation Plane
Trust Architecture
UNTRUSTED DOMAIN
├── Browser and external clients
├── AI-generated proposals
├── External model providers
└── Third-party service callbacks
          │
          ▼
SFL GATE A
├── Identity verification
├── Capability and scope validation
├── Payload and resource binding
├── Nonce and replay control
└── Default DENY policy
          │
          ▼
M7A ENFORCEMENT PLANE
├── Authorized Command Envelope
├── Captain Runtime
├── Saga Engine
└── Workflow Engine
          │
          ▼
MET47 CONTROL AND EVIDENCE PLANE
├── State transition records
├── Reconciliation events
├── Replay verification
└── Chain-linked audit evidence
No component is trusted because of its name or network location. Every component receives explicit, minimal authority and remains independently auditable.
 
Safety Invariants
The following invariants define the intended safety contract of the runtime:
Invariant	Required guarantee
ProjectionAtomicity	A projection is committed completely or not committed
SequenceConsistency	State transitions follow approved ordering rules
EpochConsistency	Temporal operations remain bound to the correct epoch
HashChainIntegrity	Evidence records preserve valid chain linkage
ReplayEquivalence	Authorized replay produces policy-equivalent behavior
BarrierConsistency	Barrier decisions remain coherent across participating layers
ActionSafety	No action exceeds its declared authority or safety boundary
Invariant status must be evidence-based:
DECLARED
  ↓
IMPLEMENTED
  ↓
TESTED
  ↓
RUNTIME_VERIFIED
Do not label an invariant ENFORCED unless an enforcement point, failure behavior, automated test, and runtime evidence are available.
 
Deterministic Control, Probabilistic Intelligence
META7 does not claim that a generative model produces identical language on every call. Determinism applies to the control plane:
•	Input and contract validation
•	Policy routing
•	State-transition eligibility
•	Idempotency behavior
•	Replay and nonce decisions
•	Evidence classification
•	Terminal-state exclusivity
•	Audit and reconciliation rules
Generative output remains advisory until it passes the declared validation and authority boundaries.
Coherence            != Truth
Interpretation       != Validation
Model consensus      != Independent evidence
Generated proposal   != Authorized action
Timeout              != Confirmed failure
Hash linkage         != Absolute immutability
 
MET47 Evidence Chain
MET47 records are designed to be tamper-evident through canonical serialization, SHA-256 hashing, and linkage to the previous record.
A typical evidence record may include:
{
  "eventType": "COMMAND_EXECUTED",
  "commandId": "cmd-001",
  "authorizationDecisionId": "authz-001",
  "principalReference": "principal-ref",
  "workflowId": "WF-001",
  "workflowRevision": "1",
  "outcome": "SUCCEEDED",
  "policyVersion": "sfl-policy-001",
  "timestamp": "<utc-timestamp>",
  "previousHash": "<previous-record-hash>",
  "hash": "<current-record-hash>"
}
Hash chaining provides tamper evidence. Stronger immutability requires protected writers, append-only storage, trusted checkpoints, independent verification, or external anchoring.
 
Replay and Reconciliation
Replay is not blind repetition. A replay operation must preserve:
•	Source command identity
•	Workflow and policy revision
•	Temporal context
•	Capability boundaries
•	Idempotency semantics
•	Evidence lineage
•	Declared side-effect policy
Unknown or partially observed outcomes enter reconciliation:
OUTCOME_UNKNOWN
     ↓
Inspect durable state
     ↓
Query external operation reference
     ↓
Compare command, evidence, and side effects
     ├── Confirm success
     ├── Confirm failure
     └── Escalate for manual review
 
Health and Readiness
The existing Captain health endpoint is:
GET /meta7/captain/health
For production deployments, separate probes are recommended:
GET /health/live
GET /health/ready
live should report process viability. ready should report whether policy, state, nonce, evidence, and required dependencies are available for safe command execution.
If a critical dependency cannot be verified, state-changing operations must fail closed.
 
Cloud Run Deployment
The API is intended to run as an immutable container revision.
GitHub repository
      ↓
Build, test, and security validation
      ↓
Immutable container image
      ↓
Artifact Registry
      ↓
Cloud Run staging
      ↓
Health, readiness, and Gate A validation
      ↓
Protected production approval
      ↓
Cloud Run production revision
      ↓
MET47 deployment evidence
Deployment Principles
•	Use GitHub OpenID Connect and Google Workload Identity Federation
•	Avoid long-lived cloud service-account keys
•	Separate deployment identity from runtime identity
•	Deploy exact commit SHAs and image digests
•	Validate staging before production promotion
•	Preserve the previous verified revision for rollback
•	Protect the production GitHub Environment with required approval
•	Store runtime secrets in an approved secret-management system
•	Never publish credentials or production connection details in documentation
Suggested Environment Configuration
GCP_PROJECT_ID
GCP_REGION
CLOUD_RUN_SERVICE
ARTIFACT_REGISTRY
WORKLOAD_IDENTITY_PROVIDER
DEPLOYER_SERVICE_ACCOUNT
RUNTIME_SERVICE_ACCOUNT
These are identifiers and configuration names. Secret values must remain outside the repository.
 
CI Quality Gates
A production candidate should pass the following sequence:
Restore
  ↓
Compile
  ↓
Unit Tests
  ↓
Invariant Tests
  ↓
Authorization Negative Tests
  ↓
Dependency and Container Scan
  ↓
Artifact Provenance
  ↓
Staging Deployment
  ↓
Runtime Smoke Test
  ↓
Replay and Reconciliation Test
  ↓
Production Approval
Recommended commands:
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
A successful build proves compilation. It does not by itself prove authorization integrity, runtime safety, ledger integrity, or production readiness.
 
Repository Map
.
├── .github/          GitHub configuration and automation
├── META7.SDK/        META7 SDK components
├── src/              Runtime and source projects
├── README.md         Project entry point
├── SECURITY.md       Security policy and reporting process
├── AcrobaticDemo.cs  Demonstration source
└── setup-github.sh   Repository setup helper
The exact project boundaries may evolve. Runtime, SDK, contracts, tests, and deployment infrastructure should remain separable even when hosted in the same repository.
 
Security
Before reporting a vulnerability, read SECURITY.md.
Do not include the following in public issues, discussions, pull requests, logs, screenshots, or documentation:
•	Passwords
•	Access tokens
•	Private keys
•	Service-account keys
•	Real nonces or capability secrets
•	Payment credentials
•	Private infrastructure endpoints
•	Sensitive customer or operational data
Security-sensitive execution paths should include negative tests, concurrency tests, crash-recovery tests, and audit verification.
 
Development Principles
Contributions should preserve these rules:
1.	Intent before execution
Every command must identify its purpose and authority.
2.	Default DENY
Missing evidence never becomes implicit permission.
3.	Immutable source identity
A revision may supersede an earlier intent, but history is not rewritten.
4.	Explicit state transitions
No hidden transition and no silent terminal state.
5.	Idempotent side effects
Retries must not duplicate business effects.
6.	Traceable transformation
Every material output must point back to its source, field, policy, and evidence.
7.	Governed intelligence
AI may recommend. Authority must authorize.
 
Roadmap
☐	Publish a stable API and compatibility policy
☐	Separate liveness and readiness endpoints
☐	Document the Gate A capability schema
☐	Publish command-envelope and evidence-envelope specifications
☐	Add invariant-focused test reports
☐	Add replay and reconciliation test fixtures
☐	Add container provenance and SBOM generation
☐	Add staging-to-production promotion controls
☐	Add signed MET47 checkpoints
☐	Publish versioned NuGet packages when the public SDK contract is stable
 
Project Status
META7 Captain M7A SDK is an evolving architecture. Endpoint availability, invariant maturity, and deployment readiness must be verified against the current source, automated tests, and runtime evidence.
Build passed           means compilation succeeded
Tests passed           means declared tests succeeded
Runtime verified       means controlled execution was observed
Security validated     means defined adversarial gates passed
Production authorized  means the designated authority approved release
 
META7 Orbital Engine
Remember the intent. Govern the field. Verify the form.
Will-Source → Will-Field → Coherence → Will-Form → Hyper-Vector
HopeCpluS Foundation
