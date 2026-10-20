# Infrastructure

Deployment assets live here while the environment-specific Compose entry points remain at
the repository root for predictable operator commands.

```text
infra/
└── docker/
    └── backend/
        └── Dockerfile
```

Runtime configuration remains under `env/dev/` and `env/prod/`. Production secrets are
injected by the deployment environment and must never be baked into container images.
