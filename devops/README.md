# DevOps

All Compose commands run from this directory. The base file defines the service graph;
the development and production overlays provide environment-specific ports, volumes,
commands, and environment files.

```bash
docker compose -f compose.yml -f compose.dev.yml up --build
docker compose -f compose.yml -f compose.prod.yml config --quiet
```

The active services are PostgreSQL, the internal Python ML service, the .NET 10 API and
orchestrator, and the React dashboard. Production secrets are intentionally blank.
