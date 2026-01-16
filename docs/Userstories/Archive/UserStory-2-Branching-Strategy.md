# Post-MVP-2: Implement Branching Strategy

## Objective

Adopt GitHub Flow with branch protection rules to ensure code quality and review processes for all changes.

## Dependencies

**Blocked by:** UserStory-1 (CI Pipeline) - Branch protection rules require functional CI checks before configuration.

## Requirements

- Use `main` as production branch
- Create feature branches with `feature/*` naming convention
- Require pull requests for all changes to `main`
- Configure branch protection rules
- Document workflow in `CONTRIBUTING.md`
- Require 1 approval for all PRs
- Automatic review requests via CODEOWNERS

## Implementation Tasks

- [x] **(User Manual)** Wait for UserStory-1 completion
  - CI workflow must exist and pass on `main` branch
  - Cannot configure branch protection without functional CI
- [x] **(Agent)** Create `CONTRIBUTING.md` with branching workflow documentation
  - Document GitHub Flow workflow
  - Document feature branch naming conventions
  - Document pull request process and review guidelines (1 approval required)
  - Link to `docs/Implementation-Guidelines.md` for code standards and testing requirements
  - Include merge requirements
- [x] **(Agent)** Create GitHub templates
  - Create `.github/PULL_REQUEST_TEMPLATE.md` with checklist (tests added, CI passing, linked issue)
  - Create issue templates in `.github/ISSUE_TEMPLATE/`:
    - Bug report template
    - Feature request template
    - Documentation template
    - Performance issue template
    - Security issue template
- [x] **(Agent)** Add `.github/CODEOWNERS` file
  - Configure `* @gjgie` to automatically request review on all PRs
- [x] **(User Manual)** Configure GitHub branch protection rules for `main`
  - Require CI passing before merge
  - Require 1 approval
  - Require branches to be up to date before merging
  - Prevent force pushes
  - Prevent deletions
  - Enable CODEOWNERS review requirement
- [x] **(Agent)** Update README.md with contribution guidelines
  - Replace basic contribution section (lines 371-377) with link to `CONTRIBUTING.md`
  - Add CI status badge

## Branch Naming Convention

- `feature/` - New features or enhancements
- `bugfix/` - Bug fixes
- `hotfix/` - Urgent production fixes
- `docs/` - Documentation-only changes

## Success Criteria

- Branch protection rules prevent direct pushes to `main`
- All changes go through pull request process with required review
- CI must pass before merge is allowed (all test suites: unit/integration, ML, E2E)
- CODEOWNERS automatically requests review from @gjgie on all PRs
- `CONTRIBUTING.md` clearly documents process with links to technical standards
- GitHub templates standardize bug reports, feature requests, and PRs
- README.md directs contributors to `CONTRIBUTING.md` and displays CI status badge
