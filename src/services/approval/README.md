# Approval Service

Dapr Workflow saga + durable HITL; owns approver-queue projection.

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.Approval.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.Approval.Application` | Use-cases over ports. |
| `ApprovalFlow.Approval.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.Approval.Api` | Host / composition root. |
| `ApprovalFlow.Approval.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
