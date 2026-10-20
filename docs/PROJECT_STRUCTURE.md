# Project structure

```text
crypto-ml-signal/
├── backend/                 Django project and domain applications
│   ├── apps/                Domain-owned models, services, APIs, and tests
│   ├── config/              Environment-independent Django configuration
│   └── manage.py
├── frontend/                React and TypeScript operations dashboard
├── infra/                   Container and deployment assets
│   └── docker/backend/      Django image definition
├── env/                     Environment-specific runtime variables
│   ├── dev/
│   └── prod/
├── src/crypto_signal/       Framework-independent ML and research package
├── tests/                   Framework-independent ML package tests
├── scripts/                 Research and maintenance utilities
├── docs/                    Architecture, safety, and operating documentation
├── compose.yml              Shared runtime topology
├── compose.dev.yml          Development-only overrides
└── compose.prod.yml         Production-only overrides
```

## Dependency direction

- `crypto_signal` never imports Django or infrastructure code.
- Django applications may call public `crypto_signal` services.
- Domain applications own their models and expose cross-domain behavior through explicit
  service functions.
- `apps.api` contains HTTP-wide behavior only; domain endpoints live with their domain.
- The React dashboard communicates only through `/api/v1/`.
- Compose chooses one environment-specific `env/` directory at a time.
