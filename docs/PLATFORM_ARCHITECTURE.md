# BAU Business Platform foundation

UCP is a product assembled from reusable platform capabilities and removable business modules. New products such as Payroll or Purchase Orders should reuse the platform instead of copying UCP-specific services.

## Shared platform capabilities

- Authentication, users, roles and capability permissions
- Organizations and user/entity assignments
- Audit and process logging
- Attachments through `IFileStorage`
- Notifications and email delivery history
- Approval links and signatures
- Reports and proof documents
- Licensing and deployment identity
- Database backup and operational health endpoints

Business modules own their entities, workflow rules, controllers and screens. They consume the shared capabilities through interfaces.

## Module contract

Modules are declared in `Platform:Modules`. Each entry has a stable key, route, dependencies and capabilities. Setting `Enabled` to `false` removes its navigation entry and makes its top-level API unavailable. This allows a deployment to include Contributions but exclude Assets without editing the application shell.

Capabilities use stable dotted identifiers such as `purchase-orders.approve`. Roles map to capabilities; modules must never hard-code a role when a capability expresses the requirement.

## Cluster rules

1. Every API node uses the same database, JWT secret, application name and deployment ID.
2. `Platform:SharedDataPath` and `FileStorage:SharedRoot` must be shared mounts visible at the same logical path on every node.
3. Data Protection keys live in the shared data path so licenses and protected email payloads are readable on every node.
4. Scheduled backup uses a PostgreSQL advisory lock, ensuring one node performs it.
5. Use `/health/live` for process liveness and `/health/ready` for dependency readiness.
6. Use `docker-compose.cluster.yml` with `docker compose -f docker-compose.cluster.yml up --build --scale api=2` for the first multi-node deployment.

The current filesystem provider is intentionally behind `IFileStorage`. Replace it with MinIO/S3/Azure Blob by adding another implementation and changing DI; business modules should not reference physical paths.

## Replication path

Start with multiple API nodes and one PostgreSQL primary. Add PgBouncer next. Only then add a streaming read replica and a `ReportingConnection` used by reports that tolerate replication delay. Approval screens, balances, audit history and post-save reads must remain on the primary.

## Remaining production hardening

- Move email from the in-memory channel to a transactional database outbox.
- Move all legacy direct filesystem calls onto `IFileStorage`.
- Send logs to Seq, Loki or OpenSearch and include the platform node ID.
- Add an S3-compatible storage provider and retention policies.
- Use Patroni or a managed PostgreSQL service for automated database failover.
