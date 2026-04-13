# UserStory-20: Small Improvements

## Status

- [x] Done

## Description

As a user, I want various small improvements across the application so that the overall experience feels more polished and consistent.

## Acceptance Criteria

- [x] Deploy script sets a display name that references the CI build number
- [x] When logged in, the "Home" navigation item is renamed to "Dashboard" with a dashboard icon
- [x] CI build is only triggered by changes in pipelines or code, not documentation
- [x] In CI, failing tests are automatically retried (at least once) before the build is marked as failed

## Tasks

- [x] Update deploy script to include the CI build number in the display name
- [x] Rename "Home" to "Dashboard" in the navigation menu when the user is logged in
- [x] Replace the home icon with a dashboard icon for the Dashboard nav item
- [x] Add path filters to CI pipeline to exclude documentation-only changes (e.g., `docs/**`, `*.md`)
- [x] Configure test retry in CI pipeline (e.g. `--retry` flag for NUnit / xUnit rerun policy)

## Notes

<!-- Add context, screenshots, or references here -->
