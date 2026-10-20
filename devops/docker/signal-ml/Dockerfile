FROM python:3.14-slim AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PYTHONPATH=/app/src
WORKDIR /app

ARG PIP_PROXY=""
COPY src/signal-ml/pyproject.toml src/signal-ml/README.md ./
COPY src/signal-ml/src ./src
RUN --mount=type=cache,target=/root/.cache/pip \
    if [ -n "$PIP_PROXY" ]; then \
        python -m pip --proxy "$PIP_PROXY" install ".[service]"; \
    else \
        python -m pip install ".[service]"; \
    fi

COPY src/signal-ml/service ./service
RUN addgroup --system app && adduser --system --ingroup app --home /app app \
    && chown -R app:app /app
USER app
EXPOSE 8001
CMD ["uvicorn", "service.main:app", "--host", "0.0.0.0", "--port", "8001"]

FROM runtime AS development
