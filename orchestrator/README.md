# .NET 10 API and orchestrator

This is the active public backend. It owns the HTTP API, authentication, users, wallets,
orders, exchange adapters, risk gates, audit events, persistence, and orchestration of
Python ML workloads.

The Python service is an internal compute boundary. It does not receive exchange secrets
and cannot place orders directly.

Run the API locally:

```bash
dotnet run --project orchestrator/src/CryptoMlSignal.Api
```

Initial endpoints:

- `GET /health/live` — process liveness.
- `GET /health/ready` — aggregate readiness including the Python ML service.
- `GET /api/v1` — versioned API discovery.
- `GET /api/v1/platform` — current service boundaries and safe operating mode.
- `GET /api/v1/ml/capabilities` — orchestrated ML capability discovery.
