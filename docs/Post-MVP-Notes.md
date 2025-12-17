# Post-MVP / Nice-to-have

Kort overzicht van features die later kunnen volgen en welke informatie Copilot nodig heeft om ze te bouwen.

MT940 parser

- Voorbeelden: provide sample MT940 files; expected mapping to Transaction fields; error handling rules.

Encrypted SQLite

- Key management approach: local passphrase vs KMS (Azure Key Vault). Rotation policy, migration path.

Backups & restore

- Backup formats (SQL dump / binary), retention policy, restore testing steps; UI for exports/imports.

Charts & dashboards

- Metrics to surface (spending per category, monthly cashflow), frontend chart library preference (Chart.js, D3, etc.).

CI/CD

- GitHub Actions templates: build, test, publish; secrets handling for DB connections; CD strategy (containers or Azure App Service).

UI Tests

- Playwright targets (Chrome/Edge), BUnit scope for Blazor components; sample test cases and fixtures.

Definition of Done

- For each nice-to-have include sample inputs, security considerations, and acceptance criteria.
