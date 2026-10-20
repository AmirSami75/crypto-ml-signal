# ADR 0001: Django and React trading platform architecture

- Status: Accepted for implementation
- Date: 2026-08-17
- Decision owners: Project owner and implementation team
- Related tracker item: `PLAT-002`

## Context

The repository currently provides a research-only Python package and CLI. It downloads
completed market candles, engineers causal features, trains and evaluates a model, runs a
cost-aware backtest, and emits a `BUY`, `HOLD`, or `SELL` research signal. It deliberately
does not manage users, persist operational state in a database, or place exchange orders.

The target product adds an authenticated API, a React operations dashboard, durable jobs,
model and signal history, paper trading, wallet and position monitoring, risk controls,
and eventually restricted real-order execution on multiple exchanges.

Real-order execution creates a materially different safety boundary. A model prediction
must never be treated as sufficient authorization to trade, and an HTTP request must never
directly perform a long training job or an uncontrolled exchange mutation.

## Decision

Build a modular monolith around the existing `crypto_signal` package:

- Django is the backend application and system-of-record boundary.
- Django REST Framework provides the versioned HTTP API.
- React with TypeScript provides the operator dashboard.
- PostgreSQL stores durable application and trading state.
- Celery workers execute long-running and retryable background work.
- Redis is the initial Celery broker and short-lived coordination store.
- The existing `crypto_signal` package remains framework-independent.
- Exchange support is implemented behind one normalized adapter contract per exchange.
- A separate risk decision is required before an order intent can reach an adapter.
- The initial operating mode is paper-only. Sandbox and live modes require explicit gates.

The first deployment remains one repository and one release artifact, but it runs as
separate processes. This gives us clear safety and scaling boundaries without introducing
premature distributed-service complexity.

## Repository shape

The planned monorepo layout is:

```text
crypto-ml-signal/
├── backend/                 Django project and Django applications
├── frontend/                React and TypeScript dashboard
├── infra/                   Container and deployment assets
├── env/                     Environment-specific runtime configuration
├── src/crypto_signal/       Existing framework-independent ML package
├── tests/                   Existing package tests
├── docs/adr/                Architecture decisions
├── config*.toml             Research CLI configuration
├── compose.yml              Shared runtime topology
├── compose.dev.yml          Development overrides
└── compose.prod.yml         Production overrides
```

The current CLI remains supported during the migration. Django code may call public
application services in `crypto_signal`; `crypto_signal` must not import Django models,
settings, Celery, React assets, or exchange credentials.

## Runtime topology

The initial runtime consists of these processes:

| Process | Responsibility |
|---|---|
| Django web | Authentication, authorization, validation, commands, queries, admin, API |
| Celery worker | Downloads, training, inference, reconciliation, alerts, scheduled jobs |
| Celery beat | Schedules candle-close inference, health checks, and reconciliation |
| React frontend | Operator workflows, monitoring, charts, approvals, incident visibility |
| PostgreSQL | Durable source of truth |
| Redis | Job broker and ephemeral coordination; never the durable order ledger |
| Market stream worker | Public prices and authenticated order/fill events |

Workers and the web process use the same domain services and database constraints. The
web process creates durable commands or job records; it does not wait for training,
historical downloads, or exchange execution to finish.

## Django application boundaries

The backend is divided by domain rather than by technical layer:

| Application | Owns |
|---|---|
| `identity` | Users, roles, API access, operator permissions |
| `audit` | Append-only security and operational audit events |
| `jobs` | Durable background-job state and execution attempts |
| `markets` | Exchanges, instruments, candle metadata, price freshness |
| `models` | Model versions, training runs, metrics, artifacts, deployments |
| `signals` | Idempotent signals and their probabilities/input provenance |
| `strategies` | Strategy configuration, deployment mode, enable/disable state |
| `risk` | Limits, decisions, rejections, approvals, kill switches |
| `execution` | Order intents, orders, fills, adapter events, reconciliation |
| `portfolio` | Balances, reservations, positions, equity and wallet snapshots |
| `monitoring` | Health checks, incidents and alert delivery state |

Cross-domain operations go through application services. Django signals are not used for
critical order workflows because their implicit execution makes causality and retries hard
to audit.

## Data and money rules

- PostgreSQL is authoritative for application state.
- Exchange state is authoritative for actual exchange orders, fills, and balances.
- Reconciliation continuously compares both and records mismatches as incidents.
- Monetary quantities, prices, fees, and sizes use decimal types, never binary floats.
- Every timestamp is stored in UTC and includes the exchange timestamp when available.
- Model versions, signals, risk decisions, order intents, orders, and fills are immutable
  records or append-only state transitions where practical.
- Raw exchange payloads are retained in a bounded, redacted form for diagnosis and audit.
- Redis loss must delay work but must not lose the durable intent or trading record.

## Signal-to-order workflow

The allowed causal sequence is:

```text
confirmed closed candle
    -> immutable model signal
    -> strategy decision
    -> immutable order intent
    -> independent risk evaluation
    -> optional human approval
    -> execution worker
    -> exchange adapter
    -> exchange acknowledgement/events
    -> fills, balances and positions
    -> reconciliation and monitoring
```

The model cannot call an exchange adapter. The React frontend cannot call an exchange
directly. Django Admin cannot bypass the order-intent and risk workflow.

## Idempotency and ambiguous outcomes

- A signal is unique by model version, instrument, timeframe, and completed candle.
- Every order intent and exchange submission receives a stable client order identifier.
- Duplicate task delivery must return the existing result rather than create another order.
- Read operations may retry with bounded exponential backoff.
- An ambiguous order submission is reconciled by client identifier before any retry.
- Blind retries of exchange order mutations are forbidden.
- State transitions are validated and protected by database transactions and constraints.

## Operating modes

All strategy, signal, credential, order, fill, balance, and position records carry an
explicit mode:

- `PAPER`: locally simulated execution; default mode.
- `SANDBOX`: an exchange-provided test environment.
- `LIVE`: real funds; disabled until all promotion gates pass.

Data from different modes cannot share a portfolio or be combined in performance metrics.
The current mode must be prominent in the dashboard and audit log.

## Security boundaries

- Exchange credentials are encrypted or provided by a secret manager.
- Secrets are never returned by the API, rendered in the frontend, or written to logs.
- Initial exchange keys are read-only; later trading keys must have withdrawals disabled.
- Permissions separate administrators, operators, analysts, and read-only viewers.
- Sensitive commands require re-authentication or a second operator where appropriate.
- Every change to strategy state, limits, credentials, approvals, or kill switches is
  audited with actor, request ID, timestamp, object, and outcome.
- Any uncertain state fails closed: no new order is sent.

## API and frontend boundary

The API is versioned under `/api/v1/`. React consumes only the API and authenticated
server-sent or WebSocket updates. It does not access PostgreSQL, Redis, model files, or
exchange APIs directly.

Django Admin is an internal, model-centric inspection tool. The React application owns
process-oriented workflows such as health monitoring, order approval, portfolio views,
incident handling, and emergency controls.

## Deployment and observability

- Local development uses containers for PostgreSQL and Redis; the Python and frontend
  processes may run in containers or directly during early development.
- Production processes share one version identifier and expose health/readiness checks.
- Logs are structured and include a request, job, signal, intent, and order correlation ID
  when applicable.
- Metrics cover job latency/failures, candle freshness, model freshness, signal frequency,
  order state, reconciliation drift, exchange connectivity, exposure, and drawdown.
- Database backups, secret rotation, incident runbooks, and restore drills are required
  before live mode.

## Promotion constraints

Implementation does not imply permission to trade. Promotion is gated in this order:

1. Reproducible platform, permissions, audit trail, and durable jobs.
2. Deterministic paper broker, accounting invariants, and reconciliation soak tests.
3. One exchange adapter with contract and failure-injection tests.
4. Independent risk limits, freshness checks, kill switch, and manual approval.
5. Continuous sandbox/testnet operation with tested alerts and runbooks.
6. Read-only production reconciliation.
7. Minimum-size, spot-only live orders with manual approval.
8. A separate evidence-based decision before any bounded automation.

## Consequences

### Positive

- Django provides the ORM, migrations, permissions, admin, and transactional boundaries
  needed by the broader product.
- The modular monolith is simpler to operate than microservices while retaining explicit
  process and domain boundaries.
- The existing research code remains testable and usable without the web platform.
- Durable intents, idempotency, and reconciliation make retries and failures auditable.
- Exchange adapters can evolve independently without leaking exchange details everywhere.

### Costs and trade-offs

- Django, PostgreSQL, Redis, Celery, and React add operational complexity compared with the
  current CLI.
- Real-time updates need a stream worker and frontend event mechanism in addition to REST.
- Strong audit and state-machine rules require more deliberate schema and test design.
- A normalized adapter cannot erase exchange-specific precision, ordering, or error rules;
  those details must remain visible and tested.

## Alternatives considered

### Flask backend

Rejected for the target platform because authentication, permissions, migrations, admin,
and database conventions would require substantially more custom assembly.

### FastAPI as the primary backend

Not selected as the primary application framework because this product is dominated by
durable business state, permissions, admin workflows, and relational data. FastAPI remains
an option for a separately justified inference service if throughput later requires it.

### Microservices from the start

Rejected as premature. Separate processes and strict module boundaries give the required
safety properties without distributed transactions, multiple deployments, and additional
failure modes.

### Generic exchange library as the only execution layer

Rejected for live execution. Shared libraries may assist research or prototypes, but each
live adapter must expose and test the selected exchange's authentication, precision,
minimum-size, rate-limit, order-state, and ambiguous-error behavior.

## Follow-up decisions

Separate ADRs are required before:

- choosing the first exchange;
- defining the concrete order state machine;
- choosing credential encryption/secret management;
- enabling sandbox order submission;
- enabling any live credential or automated execution.
