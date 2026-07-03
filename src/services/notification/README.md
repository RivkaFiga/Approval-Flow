# Notification Service

Live status projection + plain-language reason (F2).

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.Notification.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.Notification.Application` | Use-cases over ports. |
| `ApprovalFlow.Notification.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.Notification.Api` | Host / composition root. |
| `ApprovalFlow.Notification.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
