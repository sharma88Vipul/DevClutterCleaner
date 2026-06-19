# Enterprise Roadmap

The Community edition implementation now exposes the domain and service seams needed for future commercial editions:

- `LicenseStatus` reports edition, telemetry, policy sync, and role separation flags.
- Telemetry defaults to disabled.
- Audit events are written locally as JSON lines.
- Cleanup remains local and explicit.

Not implemented yet:

- Central policy synchronization.
- Role-based administration.
- SaaS fleet dashboard.
- License activation service.
- Remote device management.

These remain outside the community MVP and should be delivered only after scanner accuracy, reporting, and signed installer distribution are stable.
