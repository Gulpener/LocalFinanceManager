# Copilot instructions (minimal)

Doel

- Geef Copilot precies genoeg informatie om elk MVP te implementeren zonder extra details.

Algemene regels

- Gebruik EF Core voor persistence en Blazor (pages) voor UI.
- **Database Migrations:** Code-first EF Core met automatic migrations. Migrations worden automatisch toegepast bij app startup via `Database.MigrateAsync()` in `Program.cs`. Geen handmatige CLI-stappen vereist tijdens development.
- Tests: unit tests + integration tests met in-memory SQLite + **e2e tests met Playwright**.
- Valuta: ISO-4217 (3 letters). Geldwaarden als decimal(18,2).
- Copilot moet **altijd** voorbeeld CLI-commando's opnemen om .NET solutions en projecten aan te maken en te koppelen (bijv. `dotnet new`, `dotnet sln add`, `dotnet new blazorserver`, `dotnet add package`).
- **E2E Tests:** Copilot MOET ALTIJD bijbehorende e2e tests genereren voor nieuwe features. Use NUnit + Microsoft.Playwright in `tests/LocalFinanceManager.E2E/`. Scaffold met: `dotnet new nunit -n LocalFinanceManager.E2E -o tests/LocalFinanceManager.E2E`, `dotnet sln add tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj`, `dotnet add tests/LocalFinanceManager.E2E package Microsoft.Playwright` en `dotnet add tests/LocalFinanceManager.E2E package Microsoft.Playwright.NUnit`.

Verplichte CLI-gebruik (NOODZAKELIJK)

- Copilot MAG NOOIT handmatig `.csproj`-bestanden aanmaken of volledig handmatig projectbestanden scaffolden.
- Voor het aanmaken of wijzigen van projecten moet Copilot eerst de exacte `dotnet`-CLI-commando's tonen die lokale ontwikkelaars kunnen uitvoeren.
- Als Copilot aanpassingen aan projectbestanden nodig acht, moet het eerst de corresponderende `dotnet`-CLI-aanpak voorstellen (bijv. `dotnet new`, `dotnet add package`, `dotnet sln add`). Alleen wanneer een CLI-oplossing niet mogelijk is, mag Copilot aanvullende handmatige wijzigingen voorstellen — en dan altijd met een duidelijke motivatie.

Voorbeeld (vereist bij scaffolding):

```powershell
dotnet new blazorserver -n LocalFinanceManager
dotnet new sln -n LocalFinanceManager
dotnet sln add LocalFinanceManager/LocalFinanceManager.csproj
dotnet add LocalFinanceManager package Microsoft.EntityFrameworkCore.Sqlite
dotnet add LocalFinanceManager package Microsoft.EntityFrameworkCore.Design
dotnet add LocalFinanceManager package FluentValidation.AspNetCore
dotnet add LocalFinanceManager package IbanNet
dotnet add LocalFinanceManager package Swashbuckle.AspNetCore
```

Definition of Done (kort)

- Voor elk MVP: werkende API + minimale Blazor UI, unit + integration tests (in-memory SQLite), e2e tests, voorbeeld seed-data.
- Automatic migrations toegepast bij app startup zonder handmatige CLI-stappen.

Waar nodig voor implementatie: raadpleeg de gedetailleerde MVP-documenten in `docs/`.
