# UserStory-20: Small Improvements

## Status

- [ ] In Progress

## Description

As a user, I want various small improvements across the application so that the overall experience feels more polished and consistent.

## Acceptance Criteria

- [ ] Deploy script sets a display name that references the CI build number
- [ ] When logged in, the "Home" navigation item is renamed to "Dashboard" with a dashboard icon
- [ ] CI build is only triggered by changes in pipelines or code, not documentation
- [ ] In dark mode, text using `#212529` or `#212529BF` is replaced with a theme-aware color (e.g. `var(--bs-body-color)`, `var(--bs-secondary-color)`, or Bootstrap utilities `text-body` / `text-body-secondary`)
- [ ] In CI, failing tests are automatically retried (at least once) before the build is marked as failed

## Tasks

- [ ] Update deploy script to include the CI build number in the display name
- [ ] Rename "Home" to "Dashboard" in the navigation menu when the user is logged in
- [ ] Replace the home icon with a dashboard icon for the Dashboard nav item
- [ ] Add path filters to CI pipeline to exclude documentation-only changes (e.g., `docs/**`, `*.md`)
- [ ] Find all hardcoded `#212529` and `#212529BF` color usages and replace with a CSS variable or Bootstrap utility that adapts to dark mode
- [ ] Configure test retry in CI pipeline (e.g. `--retry` flag for NUnit / xUnit rerun policy)

## Notes

<!-- Add context, screenshots, or references here -->
