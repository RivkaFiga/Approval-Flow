# ApprovalFlow.Contracts

**The only code shared across service boundaries.**

Holds the published language of the platform: CloudEvent payload schemas
(`invoice.submitted`, `decision.made`, `review.status`, `item.finalized`),
service-invocation request/response DTOs, and shared enums (routes, statuses).

Rules:
- Contracts are versioned (`type` + `schemaVersion`); consumers ignore unknown fields.
- **No** domain logic, no infrastructure, no framework dependencies.
- Every service may reference this project; services never reference each other.
