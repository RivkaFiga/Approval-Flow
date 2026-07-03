# ApprovalFlow.Gateway (API Gateway — YARP)

Single external entry point (M6). Responsibilities: reverse-proxy routing to
services via Dapr, **rate limiting** (shared Redis counters), JWT validation and
role mapping (submitter / approver / admin), and correlation-id issuance.

Independently deployable. References only `ApprovalFlow.Contracts`.
