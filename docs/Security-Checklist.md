# Security Checklist (UI/API)

Praktische checklist voor code reviews en PR’s in LocalFinanceManager.

## 1) UI Rendering & XSS

- Gebruik **geen** `data-bs-html="true"` met data die (indirect) van gebruikersinput komt.
- Gebruik tooltips als **plain text** (`title` met gewone string), tenzij HTML strikt noodzakelijk is.
- Gebruik **geen** `MarkupString` voor dynamische/ongevalideerde content.
- Gebruik **geen** `@Html.Raw(...)` of vergelijkbare raw HTML rendering voor user data.
- Laat Blazor escaping doen door default `@value` rendering te gebruiken.

## 2) JavaScript Interop

- Gebruik **geen** `eval`, `new Function`, of `document.write`.
- Roep altijd expliciete JS-functies aan via `IJSRuntime` (bijv. `showToast`).
- Geef alleen getypte parameters door; bouw geen script-strings met interpolatie.

## 3) URL & Navigation Safety

- Vermijd dynamische externe URL’s in `href`/redirects zonder validatie.
- Laat alleen interne routes toe voor navigatie waar mogelijk (`/pad/...`).
- Blokkeer protocollen zoals `javascript:`, `data:text/html`, `vbscript:` in user-invoer voor links.

## 4) API Input & Output

- Houd query/body parameters strikt getypeerd (`Guid`, `int`, `decimal`, etc.).
- Valideer input op server-side met bestaande validators.
- Return foutresponses in consistente Problem Details-stijl (RFC 7231 pattern in dit project).

## 5) Logging & Error Messages

- Log technische details server-side.
- Toon gebruikersvriendelijke fouten client-side; vermijd onnodige interne details in UI.

## 6) Quick Review Heuristics

Zoek bij review expliciet op deze patronen:

- `InvokeVoidAsync("eval"`
- `data-bs-html="true"`
- `MarkupString`
- `@Html.Raw`
- `innerHTML`
- `javascript:`

## 7) Recent Fixes (reference)

- Tooltip hardening: `MLSuggestionBadge` aangepast naar plain-text tooltip i.p.v. HTML-content.
- JS interop hardening: `Monitoring` aangepast om `showToast` direct aan te roepen i.p.v. `eval`.

---

Gebruik deze checklist als minimale baseline. Bij nieuwe componenten met rich content: eerst threat model + expliciete sanitization-keuze documenteren.
