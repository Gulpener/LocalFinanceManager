# Bug Report 2 - Hardcoded Colors Not Readable in Dark Mode

## Status

- [ ] Open

## Summary

Text using hardcoded colors `#212529` or `#212529BF` is not readable in dark mode because these are near-black values designed for light backgrounds.

## Steps to Reproduce

1. Switch the application to dark mode.
2. Observe text elements that appear nearly invisible or have very low contrast against the dark background.

## Expected Behaviour

All text is readable in both light and dark mode. Colors adapt to the active theme.

## Actual Behaviour

Text with hardcoded color `#212529` (opaque) or `#212529BF` (75% opacity) remains near-black in dark mode, making it unreadable against a dark background.

## Root Cause

Hardcoded hex color values are used instead of theme-aware CSS variables or Bootstrap utilities.

## Fix

Replace all occurrences of `#212529` and `#212529BF` with a CSS variable or Bootstrap utility that adapts to the active theme (e.g. `var(--bs-body-color)`, `var(--bs-secondary-color)`, or Bootstrap utilities `text-body` / `text-body-secondary`).

## Tasks

- [ ] Find all hardcoded `#212529` and `#212529BF` color usages in `.razor`, `.css`, and `.html` files
- [ ] Replace with a theme-aware CSS variable or Bootstrap utility class that works in both light and dark mode
- [ ] Verify readability in both light and dark mode after the fix
