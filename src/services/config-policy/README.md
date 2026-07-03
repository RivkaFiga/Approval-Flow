# ConfigPolicy Service

Serves policy.md, thresholds, FX, vendor list; hot-reloadable (F7/M13).

**Independently deployable.** Layers (dependency rule points inward):

| Project | Role |
|---|---|
| `ApprovalFlow.ConfigPolicy.Domain` | Pure business rules (no I/O). |
| `ApprovalFlow.ConfigPolicy.Application` | Use-cases over ports. |
| `ApprovalFlow.ConfigPolicy.Infrastructure` | Adapters (Dapr, Postgres, external SDKs). |
| `ApprovalFlow.ConfigPolicy.Api` | Host / composition root. |
| `ApprovalFlow.ConfigPolicy.Tests` | xUnit tests. |

Shared code is limited to `ApprovalFlow.Contracts`. No shared Domain or Infrastructure.
