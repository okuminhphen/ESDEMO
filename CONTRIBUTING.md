# Contributing

## Branches

- `main` contains reviewed, buildable code.
- `develop` is the integration branch for daily development.
- Use `feature/<short-name>`, `fix/<short-name>` or `chore/<short-name>` for changes that benefit from isolated review.

Create a feature branch from `develop`:

```powershell
git switch develop
git pull --ff-only
git switch -c feature/my-feature
```

Merge features into `develop`. Promote a tested release with a pull request from `develop` to `main`.

## Pull requests

- Explain the resulting behavior and how it was verified.
- Keep one coherent change per pull request.
- Update documentation when setup, architecture or behavior changes.
- Do not commit `.env`, credentials, build output or Docker data.
- Run backend build/tests, frontend lint/build and Compose validation before requesting review.

## Commit messages

Use short imperative messages. Conventional Commit prefixes are recommended:

```text
feat: add task creation flow
fix: handle unavailable message broker
docs: explain local migrations
chore: update PostgreSQL image
```
