# UserStory-16: Deploy to Azure App Service (B1)

## Objective

Deploy the Blazor Server application to Azure App Service B1 tier using GitHub Actions,
with automated CD, health checks, and production logging. Uses €50/month recurring Azure
credits — B1 (~€13/mnd) is fully covered with budget remaining.

## Architecture

```
GitHub Actions → dotnet publish → azure/webapps-deploy@v2
                                         ↓
Azure App Service (B1, Linux, .NET 10, Always-On)
    → Supabase PostgreSQL (unchanged)
    → Azure-managed SSL / custom domain
```

## Requirements

- Azure App Service **B1** tier (Linux, .NET 10 runtime, always-on)
- GitHub Actions CD pipeline (deploy on main after CI passes)
- `appsettings.Production.json` with production log levels
- Supabase PostgreSQL (existing, no changes needed)
- Health check endpoint (already registered in `Program.cs`)
- Always-on enabled (B1 feature — prevents SignalR circuit drops)

## Why B1 and Not F1

|                   | Azure F1                | Azure B1                    |
| ----------------- | ----------------------- | --------------------------- |
| **Prijs**         | Gratis                  | ~€13/mnd                    |
| **Always-on**     | Nee (slaapt na 20 min)  | Ja                          |
| **SignalR**       | Verbreekt bij slaap     | Stabiel                     |
| **CPU limiet**    | 60 min/dag              | Geen                        |
| **Custom domain** | Nee                     | Ja                          |
| **Kosten**        | Gratis maar onbruikbaar | Gedekt door €50/mnd credits |

Azure F1 is fundamenteel ongeschikt voor Blazor Server vanwege de permanente
SignalR verbinding en de 60-minuten CPU dagslimiet. B1 wordt volledig gedekt
door de €50/maand recurring Azure credits.

## Implementation Tasks

### Phase 1: Azure Setup — **[User]**

- [x] **[User]** Open [portal.azure.com](https://portal.azure.com) en verifieer dat credits beschikbaar zijn (Subscriptions → Cost Management)
- [x] **[User]** Maak een **Resource Group** aan (bijv. `rg-localfinancemanager`)
- [x] **[User]** Maak een **App Service Plan** aan:
  - Tier: **B1** (Basic)
  - OS: **Linux**
  - Region: West Europe (of dichtstbijzijnde)
- [x] **[User]** Maak een **App Service** aan:
  - Runtime stack: **.NET 10**
  - Plan: het B1-plan hierboven
  - Naam: bijv. `localfinancemanager` (bepaalt de `azurewebsites.net` URL)
- [ ] **[User]** Schakel **Always On** in: App Service → Configuration → General Settings → Always On: **On**
- [ ] **[User]** Schakel **HTTPS Only** in: App Service → TLS/SSL Settings → HTTPS Only: **On**

### Phase 2: Environment Variables instellen — **[User]**

- [ ] **[User]** Ga naar App Service → Configuration → Application Settings en voeg toe:
  - `ASPNETCORE_ENVIRONMENT` = `Production`
  - `ConnectionStrings__Local` = (Supabase PostgreSQL connection string)
  - `Supabase__Url` = (Supabase project URL)
  - `Supabase__AnonKey` = (Supabase anon/public key)
- [ ] **[User]** Klik **Save** en verifieer dat de app herstart

> **Let op:** Azure App Service slaat Application Settings encrypted op.
> Kopieer nooit secrets naar `appsettings.json` of `appsettings.Production.json`.

### Phase 3: GitHub Secrets configureren — **[User]**

- [ ] **[User]** Download Publish Profile: App Service overzicht → **Get Publish Profile** → sla `.PublishSettings` file op
- [ ] **[User]** Voeg GitHub repository secrets toe (Settings → Secrets and Variables → Actions):
  - `AZURE_WEBAPP_NAME` — naam van de App Service (bijv. `localfinancemanager`)
  - `AZURE_WEBAPP_PUBLISH_PROFILE` — volledige inhoud van het `.PublishSettings` bestand

### Phase 4: Application Configuration

- [ ] **[Copilot]** Create `appsettings.Production.json`:
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "LocalFinanceManager": "Information"
      }
    }
  }
  ```
- [ ] **[User]** Verify production guard in `Program.cs` rejects localhost connection strings (already present)
- [ ] **[User]** Verify health check at `/health` is registered in `Program.cs` (already present)

### Phase 5: GitHub Actions CD Pipeline

- [ ] **[Copilot]** Create `.github/workflows/deploy.yml`:

  ```yaml
  name: Deploy to Azure

  on:
    workflow_run:
      workflows: ["CI"]
      types: [completed]
      branches: [main]

  jobs:
    deploy:
      if: ${{ github.event.workflow_run.conclusion == 'success' }}
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4

        - name: Setup .NET
          uses: actions/setup-dotnet@v4
          with:
            dotnet-version: "10.0.x"

        - name: Publish
          run: dotnet publish LocalFinanceManager/LocalFinanceManager.csproj -c Release -o ./publish

        - name: Deploy to Azure App Service
          uses: azure/webapps-deploy@v2
          with:
            app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
            publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
            package: ./publish
  ```

### Phase 6: Custom Domain (optioneel) — **[User]**

- [ ] **[User]** App Service → Custom Domains → **Add custom domain**
- [ ] **[User]** Voeg bij Vimexx een `CNAME`-record toe: naam = `finance`, waarde = `<appname>.azurewebsites.net`
- [ ] **[User]** Verifieer domein in Azure portal (volg de TXT-record instructies)
- [ ] **[User]** Azure voegt automatisch een gratis Managed SSL-certificaat toe

> Zonder custom domain is de app direct beschikbaar op `https://<appname>.azurewebsites.net`.

### Phase 7: Post-Deployment Verification — **[User]**

- [ ] **[User]** Eerste deploy triggeren: push naar `main` of herstart workflow handmatig
- [ ] **[User]** Health check: `https://<appname>.azurewebsites.net/health` → 200 OK
- [ ] **[User]** Database migrations automatisch toegepast op startup (via `MigrateAsync`)
- [ ] **[User]** Login/authenticatie via Supabase werkt
- [ ] **[User]** Blazor Server circuit verbindt (geen WebSocket fouten in browser DevTools → F12)
- [ ] **[User]** Alle pagina's laden correct
- [ ] **[User]** Controleer App Service → Log stream voor eventuele opstartfouten

## Ownership Split

| Task                             | Owner                    |
| -------------------------------- | ------------------------ |
| Azure resource provisioning      | **User** (Azure portal)  |
| Always-on + HTTPS-only instellen | **User** (Azure portal)  |
| Application Settings (secrets)   | **User** (Azure portal)  |
| Publish Profile downloaden       | **User** (Azure portal)  |
| GitHub Secrets configureren      | **User** (GitHub portal) |
| Custom domain + DNS bij Vimexx   | **User**                 |
| `appsettings.Production.json`    | **Copilot**              |
| `.github/workflows/deploy.yml`   | **Copilot**              |

## Kosten Overzicht

| Component                       | Kosten       |
| ------------------------------- | ------------ |
| Azure App Service B1            | ~€13/mnd     |
| Supabase PostgreSQL (free tier) | €0           |
| Azure-managed SSL certificate   | €0           |
| **Totaal**                      | **~€13/mnd** |
| Azure credits (recurring)       | **€50/mnd**  |
| **Resterende credits**          | **~€37/mnd** |

## Success Criteria

- Application is publicly accessible via HTTPS
- SignalR (Blazor Server circuit) stays connected — Always-on prevents sleep
- CD pipeline automatically deploys on main branch commits after CI passes
- Health check at `/health` reports 200 OK
- Supabase authentication works in production
- Database migrations run automatically on startup
- Monthly cost ~€13 fully covered by €50 recurring Azure credits

## Estimated Effort

**1-2 days** (geen Docker, Nginx of server management nodig)

## Dependencies

- **UserStory-12** (Supabase PostgreSQL) — REQUIRED — production database provider
- **UserStory-11** (Multi-User Auth) — OPTIONAL — authentication works without it, but user isolation requires it
