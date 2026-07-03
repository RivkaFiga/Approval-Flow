# Intake Service

Async accept, dedup index + outbox (one Postgres tx), publishes invoice.submitted.

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.Intake.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.Intake.Application` | Use-cases over ports. |
| `ApprovalFlow.Intake.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.Intake.Api` | Host / composition root. |
| `ApprovalFlow.Intake.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
