# Python ML service

This internal service owns data preparation, feature construction, model training,
inference, backtesting, and artifact production. It reuses the research package under
`src/crypto_signal`.

It must not receive exchange API secrets or place orders. The .NET orchestrator owns all
wallet, risk, approval, and exchange execution decisions.

Run it locally after installing the service extra:

```bash
python -m pip install -e '.[service]'
uvicorn ml.service.main:app --host 0.0.0.0 --port 8001 --reload
```

Initial endpoints:

- `GET /health`
- `GET /api/v1/capabilities`
