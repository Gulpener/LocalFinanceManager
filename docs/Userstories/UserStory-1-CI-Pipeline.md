# Post-MVP-1: Add CI Pipeline

## Objective

Create a continuous integration pipeline to automatically build, test, and validate code quality on every pull request and main branch commit.

## Requirements

- Create `.github/workflows/ci.yml` workflow file
- Build solution with `dotnet build`
- Run unit/integration tests in `LocalFinanceManager.Tests/`
- Run E2E tests with Playwright in `LocalFinanceManager.E2E/`
- Run ML tests in `LocalFinanceManager.ML.Tests/`
- Enforce code quality checks
- Run on pull requests and main branch commits

## Implementation Tasks

- [ ] Create `.github/workflows/` directory structure
- [ ] Configure `ci.yml` workflow with .NET 10 setup
- [ ] Add build step with error handling
- [ ] Configure test runners for all test projects
- [ ] Add Playwright installation step for E2E tests
- [ ] Configure code quality checks (warnings as errors, format validation)
- [ ] Set up workflow triggers for PRs and main branch
- [ ] Add status badge to README.md

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
