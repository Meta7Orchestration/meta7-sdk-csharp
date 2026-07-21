
# META7 Captain M7A SDK

> **จดจำไว้ แล้วไปด้วยกัน**

Deterministic Cognitive Runtime — META7 / QHCU / CAIM

[![Build](https://github.com/HopeCpluS-Foundation/meta7-sdk-csharp/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/HopeCpluS-Foundation/meta7-sdk-csharp/actions)

---

## 🚀 Quick Start

### Web API (Cloud Run / Docker)

```bash
docker build -t meta7-sdk .
docker run -p 8080:8080 meta7-sdk
```

Then open: http://localhost:8080/swagger

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | System info |
| GET | `/meta7/captain/status` | Full system status |
| GET | `/meta7/captain/health` | Health check |
| GET | `/meta7/captain/layers` | Layer information |
| POST | `/meta7/captain/execute` | Execute command |
| POST | `/meta7/saga/run` | Run Saga workflow |
| POST | `/meta7/workflow/execute` | Execute workflow |
| GET | `/swagger` | API documentation |

### Execute Command Example

```bash
curl -X POST https://meta7.hopecplus.com/meta7/captain/execute \
  -H "Content-Type: application/json" \
  -d '{
    "layer": "core",
    "command": "ANALYZE",
    "payload": "Hello META7"
  }'
```

### Run Saga Example

```bash
curl -X POST https://meta7.hopecplus.com/meta7/saga/run \
  -H "Content-Type: application/json" \
  -d '{
    "workflowId": "WF-001",
    "steps": ["PLAN", "EXECUTE", "VERIFY", "REPORT"],
    "stopOnFailure": true
  }'
```

---

## 🏗️ Architecture

```
META7 Captain M7A SDK — 30 Layers
│
├── Layers 1-5:   Core Types & Strategic Intelligence
├── Layers 6-10:  M7A Strategic Commander
├── Layers 11-15: Strategic Cognitive Loop
├── Layers 16-20: Command Pipeline & Gateway
├── Layers 21-25: Barrier Network MB-SYNC-001
├── Layers 26-30: Audit Ledger + Replay Engine
│
├── Saga Orchestration Engine (M7A-SAGA-1.0.0)
└── Workflow Engine V3
```

## 🛡️ Safety Invariants

- `ProjectionAtomicity` — ENFORCED
- `SequenceConsistency` — ENFORCED
- `EpochConsistency` — ENFORCED
- `HashChainIntegrity` — ENFORCED
- `ReplayEquivalence` — ENFORCED
- `BarrierConsistency` — ENFORCED
- `ActionSafety` — ENFORCED


*META7 Orbital Engine v1.4 — HopeCpluS Foundation*
