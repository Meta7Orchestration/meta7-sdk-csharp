# META7 SDK — C#

**จดจำไว้ แล้วไปด้วยกัน** 🛡️

> *Remember, and go together.*

---

## Overview

The META7 C# SDK contains:

| Project | Description |
|---|---|
| `src/META7.Operator.Contracts` | Public DTOs and enums shared across the approval pipeline |
| `src/META7.Operator.Api` | ASP.NET Core Web API — Human Approval Layer + execution pipeline |
| `tests/META7.Operator.Api.Tests` | Deterministic integration tests for the approval layer |
| `src/META7.CaptainM7A.Core.cs` | Captain M7A SDK — Layers 1–15 |
| `src/META7.CaptainM7A.Layer16to20.cs` | Captain M7A SDK — Layers 16–20 |
| `src/META7.CaptainM7A.Layer21to30.cs` | Captain M7A SDK — Layers 21–30 |

---

## Human Approval Layer

### What it does

The **Human Approval Layer** is a mandatory, human-in-the-loop checkpoint that sits in front of every write-capable action produced by the Autonomous Revenue Engine.

**No write action is ever executed without explicit human approval.**

### Architecture

```
Autonomous Revenue Engine
         │
         ▼
DirectiveExecutionService
  ├─ SAFE_LOCK check          ← blocks everything when active
  ├─ IsWriteAction() check    ← read-only actions pass through directly
  └─ HumanApprovalGateway     ← write actions intercepted here
              │
              ▼ (Pending)
       HumanApprovalQueue
              │
              ▼ (human action via REST)
  HumanApprovalController
    ├─ GET  /v1/approvals/pending
    ├─ POST /v1/approvals/{id}/approve  → DirectiveExecutionService.ExecuteApproved()
    └─ POST /v1/approvals/{id}/reject   → action permanently blocked
```

### Write-capable actions (ALL require human approval)

| Action | Enum value | Description |
|---|---|---|
| `SubmitLeadForm` | 100 | Submits a lead form to an external target |
| `RequestCallback` | 101 | Requests a human callback |
| `TriggerWebhook` | 102 | Triggers an outbound webhook |
| `CreateSupportTicket` | 103 | Creates a support ticket in an external system |
| `RegisterInterest` | 104 | Registers interest in an external product/service |

### Safety guarantees

1. **All write actions require human approval** — the gateway cannot be bypassed.
2. **No auto-approval** — the queue never self-approves or auto-expires requests.
3. **No auto-execution** — `DirectiveExecutionService` only calls the executor after a human explicitly calls `POST /v1/approvals/{id}/approve`.
4. **SAFE_LOCK enforcement** — when `META7_SAFE_LOCK=true`, all write submissions AND post-approval executions are blocked.
5. **Immutability** — once a request is approved or rejected, its record is permanently sealed and cannot be changed.
6. **Domain allowlist** — `HumanApprovalGateway` optionally enforces an allowlist of permitted originating domains.

### API endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/v1/approvals/pending` | List all write-action requests awaiting human decision (oldest first) |
| `POST` | `/v1/approvals/{id}/approve` | Approve a specific request; triggers execution via safety pipeline |
| `POST` | `/v1/approvals/{id}/reject` | Reject a specific request; action will never be executed |

#### Approve example

```http
POST /v1/approvals/REQ-001/approve
Content-Type: application/json

{
  "approvedBy": "operator@meta7.io"
}
```

#### Reject example

```http
POST /v1/approvals/REQ-001/reject
Content-Type: application/json

{
  "reason": "Not approved by policy review"
}
```

### SAFE_LOCK

Set the environment variable `META7_SAFE_LOCK=true` to engage SAFE_LOCK mode.
While active:
- All write-action submissions are immediately rejected.
- All post-approval executions are blocked, even for already-approved requests.

```bash
META7_SAFE_LOCK=true dotnet run --project src/META7.Operator.Api
```

### Data model

```
HumanApprovalRecord
├── RequestId    (string, GUID)
├── DirectiveId  (string)
├── ActionType   (OperatorActionType)
├── Payload      (string, serialised action data)
├── Domain       (string?, optional originating domain)
├── CreatedAt    (DateTime, UTC)
├── ApprovedAt   (DateTime?, null until approved)
├── RejectedAt   (DateTime?, null until rejected)
├── ApprovedBy   (string?,  null until approved)
├── RejectionReason (string?, optional)
└── Status       (HumanApprovalStatus: Pending | Approved | Rejected)
```

---

## Build & Test

```bash
# From repository root:
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Expected output:
```
Build succeeded.    0 Warning(s)    0 Error(s)
Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26
```

---

## What's next — PR #10

PR #10 will introduce the **Autonomous Revenue Loop**, which produces write-action directives. Every directive it generates will automatically flow through this Human Approval Layer before any external state is modified.

---

## Captain M7A SDK

The `src/META7.CaptainM7A.*.cs` files contain the Captain M7A multi-agent orchestration SDK (Layers 1–30). This SDK is independent of the Operator API and Human Approval Layer.

---

*META7 Orchestration — All write actions require human approval.*
