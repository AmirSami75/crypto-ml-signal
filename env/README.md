# Environment files

Compose loads configuration only from the matching environment directory:

- `dev/` contains safe local-development defaults.
- `prod/` contains intentionally blank secret placeholders and must be populated by the
  deployment secret-management process before production starts.

The `seed` service creates the configured Django superadmin after migrations. Development
ships with a local-only account in `dev/app.env`. Production skips seeding until all three
`DJANGO_SUPERUSER_*` variables are injected by the deployment secret manager.

The seed is idempotent and does not reset an existing password. To apply a deliberate
password rotation, run `python manage.py seed_superadmin --reset-password` in a one-off
backend container after updating the secret.

Never reuse the development database password or Django secret in another environment.
Never reuse the development superadmin password outside the local Docker environment.
The production database is not published to the host; only containers on the Compose
application network can reach it.
