# Contributing Guide

Thank you for contributing to UCB Hold. This project favors small, reviewable changes, clear intent and consistent implementation over large unstructured updates.

## Development Workflow

1. Create a focused branch from `main`.
2. Keep each change scoped to one user-facing feature, fix, refactor or documentation update.
3. Follow the existing architecture before introducing new patterns.
4. Update documentation when behavior, setup, deployment, API contracts or database structure changes.
5. Run the local quality checks before opening a pull request.

## Branch Naming

Use short lowercase branch names:

| Type     | Example                      |
| -------- | ---------------------------- |
| Feature  | `feature/comment-likes`      |
| Fix      | `fix/mobile-notifications`   |
| Refactor | `refactor/repository-layout` |
| Docs     | `docs/release-notes`         |
| CI       | `ci/coverage-artifacts`      |

## Commit Standard

Use Conventional Commits:

```text
feat(scope): add user comment replies
fix(scope): correct mobile notification counter
refactor(scope): normalize repository layout
docs(scope): update setup guide
test(scope): cover comment permissions
ci(scope): publish coverage artifacts
chore(scope): ignore generated files
```

Prefer atomic commits. A commit should explain one reason for change and should be revertable without unrelated fallout.

## Local Quality Gates

Backend:

```bash
dotnet build code/UCB_Hold.sln
dotnet test code/tests/UCB_Hold.Tests.csproj
```

Frontend:

```bash
cd code/client
npm run format:check
npx tsc -p tsconfig.app.json --noEmit
npx tsc -p tsconfig.spec.json --noEmit
npm run test:coverage
npm run build
```

## Pull Request Requirements

A pull request should include:

| Section      | Expected content                                                                        |
| ------------ | --------------------------------------------------------------------------------------- |
| Summary      | What changed and which area was affected.                                               |
| Motivation   | Why the change is needed.                                                               |
| Verification | Commands executed and manual scenarios tested.                                          |
| Risk         | Migrations, data changes, UI impact, security considerations or compatibility concerns. |
| Screenshots  | Required for visible UI changes.                                                        |

## Code Expectations

| Area            | Standard                                                                                   |
| --------------- | ------------------------------------------------------------------------------------------ |
| Frontend        | Use existing Angular and Feature-Sliced Design boundaries. Keep files in `kebab-case`.     |
| Backend         | Keep business rules in services, validations explicit and persistence behind repositories. |
| Tests           | Add or update tests when behavior, permissions or data rules change.                       |
| Documentation   | Keep examples current and avoid environment-specific secrets.                              |
| Generated files | Do not commit local reports, build output, caches or database backups.                     |

## Dependencies

New dependencies should be justified by a clear maintenance or product benefit. Include why the dependency is needed, whether a built-in alternative exists and any relevant security or licensing consideration.
