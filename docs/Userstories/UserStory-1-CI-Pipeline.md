# Post-MVP-1: Add CI Pipeline

## Objective

Create a continuous integration pipeline to automatically build, test, and validate code quality on every pull request and main branch commit.

## Requirements

- Create `.github/workflows/ci.yml` workflow file
- Build solution in Release configuration with `dotnet build --configuration Release`
- Run unit/integration tests in `LocalFinanceManager.Tests/` with code coverage
- Run ML tests in `LocalFinanceManager.ML.Tests/` with code coverage
- Run E2E tests with Playwright in `LocalFinanceManager.E2E/` with code coverage (including tests marked `[Ignore]`)
- Collect code coverage locally using `coverlet.collector` (XPlat Code Coverage format)
- Store test results and coverage reports as workflow artifacts with 30-day retention
- Run on pull requests and main branch commits
- Respect project code quality settings (warnings not as errors per Implementation-Guidelines.md)

## Prerequisites (User Manual Setup Required)

- [x] **(User Manual)** Create GitHub repository and push local code to remote
- [ ] **(User Manual)** Enable GitHub Actions in repository settings (Settings → Actions → Allow all actions)
- [x] **(User Manual)** Verify `.gitignore` excludes `bin/`, `obj/`, `*.db` files (prevents build artifacts in git)

## Technical Decisions

This user story implements CI/CD following technical decisions documented in `docs/Implementation-Guidelines.md`:

- **.NET Version:** Target framework `net10.0` for all projects
- **Test Frameworks:** NUnit for unit/integration/ML tests; NUnit + Microsoft.Playwright for E2E tests
- **Database Strategy:** Tests use in-memory SQLite (unit/integration) or `WebApplicationFactory` with dedicated test database (E2E); no external database or GitHub secrets required
- **Code Quality:** Warnings NOT treated as errors (`<TreatWarningsAsErrors>false`); CI builds without `--warnaserror` flag
- **Logging:** Test commands include `--logger "console;verbosity=normal"` for CI diagnostics
- **Playwright Installation:** `Microsoft.Playwright` NuGet package automatically installs browsers via MSBuild targets; no manual script execution needed

## Implementation Tasks

- [ ] ⚠️ **BLOCKING** — **(Agent)** Create `.github/workflows/` directory structure
- [ ] ⚠️ **BLOCKING** — **(Agent)** Configure `ci.yml` workflow with .NET 10 SDK setup using `actions/setup-dotnet@v4`
- [ ] ⚠️ **BLOCKING** — **(Agent)** Add build step: `dotnet build --configuration Release` (no --warnaserror override)
- [ ] **(Agent)** Configure test execution with coverage for LocalFinanceManager.Tests: `dotnet test tests/LocalFinanceManager.Tests/ --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"`
- [ ] **(Agent)** Configure test execution with coverage for LocalFinanceManager.ML.Tests: `dotnet test tests/LocalFinanceManager.ML.Tests/ --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"`
- [ ] **(Agent)** Add Playwright browser installation step (Microsoft.Playwright package handles installation automatically via MSBuild targets; verify by running `dotnet build tests/LocalFinanceManager.E2E/`)
- [ ] **(Agent)** Configure test execution with coverage for LocalFinanceManager.E2E (run all tests including `[Ignore]` marked tests): `dotnet test tests/LocalFinanceManager.E2E/ --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage" -- NUnit.Where="cat != Ignore"`
- [ ] **(Agent)** Add artifact upload step for test results (`.trx` files) and coverage reports (Cobertura XML format) with 30-day retention using `actions/upload-artifact@v4`
- [ ] **(Agent)** Set up workflow triggers for `push` to `main` branch and `pull_request` events
- [ ] **(Agent)** Add CI status badge to top of README.md using GitHub Actions badge format: `![CI](https://github.com/{owner}/{repo}/actions/workflows/ci.yml/badge.svg)`
- [ ] **(User Manual)** Enable branch protection rules in GitHub UI (Settings → Branches → Add rule for `main`: require CI status checks to pass before merging)

## Testing

- [ ] **(User Manual)** Push feature branch to GitHub and verify workflow runs successfully (check Actions tab)
- [ ] **(User Manual)** Create pull request and verify workflow runs automatically on PR creation
- [ ] **(User Manual)** Verify all test suites execute and report results (check GitHub Actions logs for "LocalFinanceManager.Tests", "LocalFinanceManager.ML.Tests", "LocalFinanceManager.E2E" output)
- [ ] **(User Manual)** Verify workflow fails gracefully on build errors (test by introducing syntax error, push, verify workflow shows failure)
- [ ] **(User Manual)** Verify workflow fails gracefully on test failures (test by breaking a test, push, verify workflow shows failure)
- [ ] **(User Manual)** Verify coverage artifacts uploaded successfully (download from GitHub Actions artifacts, check Cobertura XML files present)
- [ ] **(User Manual)** Verify CI status badge displays correctly in README.md (should show green checkmark for passing builds)

## Success Criteria

- CI pipeline successfully builds solution in Release configuration
- All test suites pass (unit, integration, ML, E2E) with code coverage collected
- Code quality checks enforce standards (warnings reported but not blocking)
- Pipeline results visible on GitHub pull requests with status badge in README.md
- Branch protection rules prevent merging PRs with failing CI

**Upon completion:** This user story is immediately archived to `docs/Userstories/Archive/UserStory-1-CI-Pipeline.md` after successful implementation and verification.
