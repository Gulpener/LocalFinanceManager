# Accessibility (WCAG 2.1 AA)

## Scope

This document covers keyboard accessibility enhancements for transaction assignment workflows:

- Transactions list keyboard navigation
- Assignment-related modals (`TransactionAssignModal`, `SplitEditor`, `BulkAssignModal`)
- Global shortcut help modal (`ShortcutHelp`)

## Keyboard Shortcuts

- `?` opens shortcut help
- `/` focuses the filter input
- `Esc` closes active modals/help
- `Tab` navigates controls inside modals
- `Enter` submits when save/assign button is focused
- `Space` toggles focused transaction selection
- `Ctrl+A` / `Cmd+A` selects all visible transactions
- `Ctrl+D` / `Cmd+D` deselects all visible transactions
- `Arrow Up/Down` moves focus between transaction rows
- `Home` / `End` jumps to first/last transaction row
- `Ctrl/Cmd + +` and `Ctrl/Cmd + -` add/remove split rows in split editor

## Focus Management

- Modals use `role="dialog"` and `aria-modal="true"`
- Focus is trapped within open modals (Tab wrap behavior)
- `Esc` is supported to exit modals
- Focus-visible styles use a high-contrast 2px outline
- Transaction rows are keyboard-focusable and visually highlighted on focus

## Screen Reader Considerations

- Modal titles are connected through `aria-labelledby`
- Progress text in bulk assignment uses `aria-live="polite"`
- Buttons include descriptive labels for keyboard hints (e.g., Esc close hints)

## Touch Device Behavior

- Touch devices are detected via `navigator.maxTouchPoints > 0`
- Keyboard shortcuts are not registered globally on touch devices
- Help modal shows touch gestures instead of keyboard-centric instructions

## Testing Notes

Automated checks:

- Playwright keyboard navigation tests
- Axe-core scans on transactions page and relevant modals

Manual checks still required:

- Visual focus outline confirmation in supported browsers
- Screen reader interaction checks

## Assistive Technologies

Recommended manual verification coverage:

- NVDA (Windows)
- JAWS (Windows)
- VoiceOver (macOS)
