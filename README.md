# McDoit AWS Lambda Executors

Utilities for building AWS Lambda handlers with `Microsoft.Extensions.Hosting`, including base executors plus SNS and SQS integrations.

## Packages

| Package | Purpose |
| --- | --- |
| `McDoit.Aws.Lambda.Executors` | Core hosting, registration, and execution abstractions for Lambda workloads. |
| `McDoit.Aws.Lambda.Executors.Sns` | SNS-specific registration and executor helpers. |
| `McDoit.Aws.Lambda.Executors.Sqs` | SQS-specific registration and executor helpers. |

## Local development

```powershell
dotnet test .\McDoit.Aws.Lambda.Executors.slnx
```

## Release workflow

1. Open a pull request with a Conventional Commit title (for squash merge history), for example:
   - `feat: add xyz`
   - `fix: correct abc`
   - `docs: update usage notes`
2. CI runs restore, build, and tests on each PR.
3. Merging into `main` runs Release Please, which updates/creates a release PR from commit history.
4. Merging the release PR creates the GitHub release and tag.
5. When a release is created, `release-please.yml` calls the reusable `publish-nuget.yml` workflow with the created tag.
6. `publish-nuget.yml` packs all three packages and pushes `.nupkg` + `.snupkg` files to NuGet when `dry_run` is `false`.
7. Maintainers can run `publish-nuget.yml` manually via `workflow_dispatch` (for example with `dry_run: true` for packaging-only validation).

## Required GitHub repository setup

1. Add repository secret `NUGET_API_KEY` with a NuGet.org API key allowed to publish these packages.
2. No separate Release Please PAT is required; `release-please.yml` uses `GITHUB_TOKEN` and triggers publish through `workflow_call`.
3. Enable branch protection for `main` and require these checks:
   - `CI / build-and-test`
   - `Semantic PR Title / validate`
   - `CodeQL / analyze`
4. Prefer squash merges so release commits follow PR title Conventional Commit format.

## Recommended extras

- Configure NuGet Trusted Publishing to remove long-lived API key usage when ready.
- Keep Dependabot PRs enabled for both NuGet and GitHub Actions updates.
