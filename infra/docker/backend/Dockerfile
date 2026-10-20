FROM python:3.14-slim AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PYTHONPATH=/app/src

WORKDIR /app

COPY pyproject.toml README.md LICENSE ./
COPY src ./src
ARG PIP_PROXY=""
RUN --mount=type=cache,target=/root/.cache/pip \
    if [ -n "$PIP_PROXY" ]; then \
        python -m pip --proxy "$PIP_PROXY" install ".[platform]"; \
    else \
        python -m pip install ".[platform]"; \
    fi

COPY backend ./backend

RUN DJANGO_SETTINGS_MODULE=config.settings.production \
    DJANGO_SECRET_KEY=docker-image-build-only-secret-key-not-for-runtime \
    DJANGO_ALLOWED_HOSTS=localhost \
    POSTGRES_DB=build \
    POSTGRES_USER=build \
    POSTGRES_PASSWORD=build \
    python backend/manage.py collectstatic --noinput \
    && addgroup --system app \
    && adduser --system --ingroup app --home /app app \
    && chown -R app:app /app/backend/var

USER app
WORKDIR /app/backend

EXPOSE 8000

CMD ["gunicorn", "--bind=0.0.0.0:8000", "--workers=2", "--access-logfile=-", "--error-logfile=-", "config.wsgi:application"]
