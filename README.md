# ApprovalFlow

Invoice & Expense Approval Platform — microservice solution scaffold.
See [ARCHITECTURE.md](ARCHITECTURE.md) for the authoritative design.

> This repository currently contains the **solution structure only** — projects,
> folders, references, and README placeholders. No business logic is implemented yet.

## Solution layout

```
ApprovalFlow.sln
src/
  shared/
    ApprovalFlow.Contracts/          # the ONLY shared code (events + DTOs)
  gateway/
    ApprovalFlow.Gateway/            # YARP API gateway (single entry point)
  services/
    intake/                          # async accept, dedup + outbox
    ai-decision/                     # deterministic router + advisory agent
    approval/                        # Dapr Workflow saga + durable HITL
    payment/                         # budget reserve / pay / compensate
    notification/                    # live status projection
    config-policy/                   # policy, thresholds, FX (hot-reloadable)
```

Each service is **independently deployable** and internally layered with the
clean-architecture dependency rule pointing inward:

| Layer | Project suffix | Depends on |
|---|---|---|
| Domain | `.Domain` | — (pure, no I/O, no shared Domain) |
| Application | `.Application` | Domain, Contracts |
| Infrastructure | `.Infrastructure` | Application, Domain, Contracts |
| Api (host) | `.Api` | Application, Infrastructure, Contracts |
| Tests | `.Tests` | all four of the above |

## Sharing rules (enforced by project references)

- **Shared code is limited to `ApprovalFlow.Contracts`.**
- **No shared Domain** — every service owns its own `.Domain`.
- **No shared Infrastructure** — every service owns its own `.Infrastructure`.
- Services never reference each other; cross-service communication is via
  Dapr (service invocation / pub-sub), per the architecture.

## Build

```bash
dotnet build ApprovalFlow.sln
```
