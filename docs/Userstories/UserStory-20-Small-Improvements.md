# UserStory-20: Small Improvements

## Status

- [ ] In Progress

## Description

As a user, I want various small improvements across the application so that the overall experience feels more polished and consistent.

## Acceptance Criteria

- [ ] Deploy script sets a display name that references the CI build number
- [ ] When logged in, the "Home" navigation item is renamed to "Dashboard" with a dashboard icon
- [ ] CI build is only triggered by changes in pipelines or code, not documentation

## Tasks

- [ ] Update deploy script to include the CI build number in the display name
- [ ] Rename "Home" to "Dashboard" in the navigation menu when the user is logged in
- [ ] Replace the home icon with a dashboard icon for the Dashboard nav item
- [ ] Add path filters to CI pipeline to exclude documentation-only changes (e.g., `docs/**`, `*.md`)

## Notes

<!-- Add context, screenshots, or references here -->
