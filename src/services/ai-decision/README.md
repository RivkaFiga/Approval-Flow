# AiDecision Service

Deterministic pre-checks + CategoryRules evaluator + agent (advisory) + deterministic ROUTER.

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.AiDecision.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.AiDecision.Application` | Use-cases over ports. |
| `ApprovalFlow.AiDecision.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.AiDecision.Api` | Host / composition root. |
| `ApprovalFlow.AiDecision.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
