# Payment Service

Budget reserve (ETag CAS), pay (idempotent), compensate; append-only ledger.

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.Payment.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.Payment.Application` | Use-cases over ports. |
| `ApprovalFlow.Payment.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.Payment.Api` | Host / composition root. |
| `ApprovalFlow.Payment.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
