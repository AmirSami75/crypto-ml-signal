from __future__ import annotations

import os
from datetime import UTC, datetime

from fastapi import FastAPI


app = FastAPI(
    title="Crypto ML Internal Service",
    version="1.0.0",
    docs_url="/docs" if os.environ.get("ML_ENVIRONMENT") == "development" else None,
    redoc_url=None,
)


@app.get("/health", tags=["infrastructure"])
def health() -> dict[str, object]:
    return {
        "service": "python-ml",
        "status": "healthy",
        "operatingMode": os.environ.get("OPERATING_MODE", "PAPER").upper(),
        "timestamp": datetime.now(UTC).isoformat(),
    }


@app.get("/api/v1/capabilities", tags=["orchestration"])
def capabilities() -> dict[str, object]:
    return {
        "service": "python-ml",
        "capabilities": [
            "market-data-validation",
            "feature-engineering",
            "model-training",
            "model-inference",
            "cost-aware-backtesting",
        ],
        "supportedJobTypes": [
            "DOWNLOAD_MARKET_DATA",
            "TRAIN_MODEL",
            "RUN_INFERENCE",
            "RUN_BACKTEST",
        ],
    }
