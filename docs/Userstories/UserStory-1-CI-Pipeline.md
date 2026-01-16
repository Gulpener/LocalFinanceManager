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

## Implementation Tasks

- [ ] Create `.github/workflows/` directory structure
- [ ] Configure `ci.yml` workflow with .NET 10 SDK setup using `actions/setup-dotnet@v4`
- [ ] Add build step: `dotnet build --configuration Release` (no --warnaserror override)
- [ ] Configure sequential test execution with coverage for LocalFinanceManager.Tests
- [ ] Add Playwright installation step: `pwsh tests/LocalFinanceManager.E2E/bin/Release/net10.0/playwright.ps1 install --with-deps`
- [ ] Configure test execution with coverage for LocalFinanceManager.ML.Tests
- [ ] Configure test execution with coverage for LocalFinanceManager.E2E (run all tests including `[Ignore]` marked tests)
- [ ] Add artifact upload step for test results and coverage reports with 30-day retention
- [ ] Set up workflow triggers for push to `main` and `pull_request` events
- [ ] Add CI status badge to top of README.md using GitHub Actions badge format

## Testing

- Verify workflow runs successfully on push to feature branch
- Verify workflow runs on pull request creation
- Verify all test suites execute and report results
- Verify workflow fails on build errors or test failures

## Success Criteria

- CI pipeline successfully builds solution
- All test suites pass
- Code quality checks enforce standards
- Pipeline results visible on GitHub pull requests
