# UserStory-22: Application Rename

## Story

As a developer I want to rename the application from "LocalFinanceManager" to a new name, so that the branding is consistent across the codebase, UI, infrastructure, and documentation.

## Acceptance Criteria

### 1. Name Decision

- [ ] A new application name (`[NewName]`) is chosen and agreed upon
- [ ] **This step blocks all other steps — do not start implementation before the name is decided**

### 2. Solution & Projects _(🤖 Copilot)_

- [ ] `.sln` file is renamed to `[NewName].sln`
- [ ] Main project folder `LocalFinanceManager/` is renamed to `[NewName]/`
- [ ] `LocalFinanceManager.csproj` is renamed to `[NewName].csproj`
- [ ] `LocalFinanceManager.ML/` folder and `.csproj` are renamed to `[NewName].ML`
- [ ] `tests/LocalFinanceManager.Tests/` folder and `.csproj` are renamed to `[NewName].Tests`
- [ ] `tests/LocalFinanceManager.E2E/` folder and `.csproj` are renamed to `[NewName].E2E`
- [ ] `tests/LocalFinanceManager.ML.Tests/` folder and `.csproj` are renamed to `[NewName].ML.Tests`
- [ ] All project references inside `.csproj` and `.sln` files are updated
- [ ] `.github/copilot-instructions.md` is updated with the new project structure

### 3. Namespaces _(🤖 Copilot)_

- [ ] All `namespace LocalFinanceManager` declarations in `.cs` and `.razor` files are updated to `[NewName]`
- [ ] All `using LocalFinanceManager` statements are updated
- [ ] `dotnet build` produces zero errors after the rename

### 4. UI Display Name _(🤖 Copilot)_

- [ ] Browser `<title>` tag shows the new name
- [ ] Navigation bar / layout header shows the new name
- [ ] Any hardcoded "Local Finance Manager" strings in Razor pages and components are updated

### 5. Configuration _(🤖 Copilot)_

- [ ] Logging category key in `appsettings.json`, `appsettings.Development.json`, and `appsettings.Production.json` is updated from `"LocalFinanceManager"` to `"[NewName]"`

### 6. CI/CD Pipelines _(🤖 Copilot)_

- [ ] All project path references in `.github/workflows/ci.yml` are updated (build, test, publish paths)
- [ ] All project path references in `.github/workflows/deploy.yml` are updated

### 7. README & Docs _(🤖 Copilot)_

- [ ] `README.md` title and CI badge URL are updated to reflect the new repository name
- [ ] `docs/Implementation-Guidelines.md` references to `LocalFinanceManager` are updated
- [ ] Other plan/guide docs under `docs/` are updated where they reference the old name

### 8. GitHub Repository _(👤 User)_

- [ ] Repository is renamed in GitHub → Settings → General → Repository name
- [ ] CI badge URL in `README.md` is verified to resolve after the rename

### 9. Local Git Remote _(👤 User)_

- [ ] Run `git remote set-url origin <new-github-url>` locally after the GitHub repo rename
- [ ] Communicate the new remote URL to other contributors

### 10. Azure App Service _(👤 User)_

- [ ] Azure App Service is renamed or a new App Service with the new name is created in the Azure Portal
- [ ] If recreated: deployment slots, custom domains, and TLS certificates are reconfigured
- [ ] Old App Service is deleted after successful deployment to the new one

### 11. Azure App Settings _(👤 User)_

- [ ] Connection string and any App Settings in Azure that reference the old name are updated
- [ ] Application restarts successfully after the update

### 12. GitHub Secret _(👤 User)_

- [ ] `AZURE_WEBAPP_NAME` secret in GitHub → Settings → Secrets is updated to the new App Service name

### 13. Supabase Project _(👤 User — optional)_

- [ ] Supabase project is renamed in the Supabase dashboard for cosmetic consistency (does not affect DNS or API URLs)

### 14. Verify Build & Tests _(🤖 Copilot)_

- [ ] `dotnet build` succeeds with zero errors
- [ ] `dotnet test` passes for all test projects
- [ ] App starts locally and the browser title and nav bar show the new name

### 15. Verify Deployment _(👤 User)_

- [ ] CI pipeline runs green on the renamed project paths after pushing to the main branch
- [ ] Azure deployment succeeds and the live app shows the new name

## Technical Notes

- **No database rename:** the connection string's `Database=localfinancemanager` value is intentionally kept unchanged to avoid any data migration risk. Only the application-level name changes.
- **Namespace rename approach:** use a workspace-wide find-and-replace of `LocalFinanceManager` → `[NewName]` in all `.cs` and `.razor` files. Verify with `dotnet build` immediately after.
- **Solution rename:** use `git mv` for folder renames to preserve git history.
- **Azure App Service:** in-place renaming of an App Service changes its default hostname (`*.azurewebsites.net`). If a custom domain is in use, prefer creating a new App Service under the new name and swapping traffic, then deleting the old one.
- **CI/CD order:** update the workflow files and merge to main _before_ renaming the GitHub repo to avoid a broken pipeline window.

## Tasks

### 👤 User tasks (manual steps required)

- [ ] **Decide on the new application name** _(blocks everything)_
- [ ] Rename the GitHub repository (Settings → General → Repository name)
- [ ] Update local git remote: `git remote set-url origin <new-url>`
- [ ] Rename / recreate Azure App Service with the new name
- [ ] Update Azure App Settings if they reference the old name
- [ ] Update `AZURE_WEBAPP_NAME` GitHub secret to the new App Service name
- [ ] _(Optional)_ Rename Supabase project in the Supabase dashboard
- [ ] Verify live deployment after CI/CD runs green

### 🤖 Copilot tasks

- [ ] Rename solution file and all project files + folders (using `git mv`)
- [ ] Update all `.sln` and `.csproj` project references
- [ ] Bulk-replace `LocalFinanceManager` namespaces and usings in all `.cs` and `.razor` files
- [ ] Update UI display name in Razor layout / title tag
- [ ] Update logging category keys in all `appsettings.*.json` files
- [ ] Update `.github/workflows/ci.yml` and `deploy.yml` project paths
- [ ] Update `README.md` badge URL and title
- [ ] Update `docs/Implementation-Guidelines.md` and other docs
- [ ] Update `.github/copilot-instructions.md`
- [ ] Run `dotnet build` and `dotnet test` to verify zero errors
