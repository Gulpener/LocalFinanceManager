# Post-MVP-10: Deploy to Azure App Service Free Tier

## Objective

Automate deployment to Azure App Service using GitHub Actions CD pipeline, with health checks and production logging configured.

## Requirements

- Create `.github/workflows/deploy.yml` for CD
- Configure Azure App Service (Linux, .NET 10)
- Add GitHub Secrets for credentials
- Implement health checks endpoint
- Configure production logging
- Trigger deployment only after CI passes

## Implementation Tasks

### Azure Setup
- [ ] Create Azure account (if not exists)
- [ ] Create Azure App Service:
  - Tier: Free (F1)
  - OS: Linux
  - Runtime: .NET 10
  - Region: (choose closest to users)
- [ ] Note App Service name and resource group
- [ ] Create deployment credentials (publish profile or service principal)

### GitHub Secrets Configuration
- [ ] Add GitHub repository secrets:
  - `AZURE_WEBAPP_PUBLISH_PROFILE` (from Azure portal)
  - `SUPABASE_CONNECTION_STRING` (PostgreSQL connection string)
  - `JWT_SECRET` (for token signing)
  - `SUPABASE_URL`
  - `SUPABASE_ANON_KEY`

### Health Check Endpoint
- [ ] Create `HealthController.cs`:
  ```csharp
  [HttpGet("/health")]
  public async Task<IActionResult> Health()
  {
      // Check database connectivity
      // Check Supabase auth connectivity
      // Return 200 OK with status info
  }
  ```
- [ ] Add health check middleware in `Program.cs`:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddNpgSql(connectionString)
      .AddCheck("supabase", ...);
  app.MapHealthChecks("/health");
  ```

### Production Logging Configuration
- [ ] Update `appsettings.Production.json`:
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
- [ ] Add Application Insights (optional):
  - Install `Microsoft.ApplicationInsights.AspNetCore`
  - Configure in `Program.cs`
  - Add instrumentation key to secrets

### CD Pipeline
- [ ] Create `.github/workflows/deploy.yml`:
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
            dotnet-version: '10.0.x'
        - name: Publish
          run: dotnet publish LocalFinanceManager/LocalFinanceManager.csproj -c Release -o ./publish
        - name: Deploy to Azure
          uses: azure/webapps-deploy@v2
          with:
            app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
            publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
            package: ./publish
  ```
- [ ] Configure workflow to only run after CI succeeds
- [ ] Add deployment status notifications (optional)

### Azure App Service Configuration
- [ ] Set environment variables in Azure portal:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__Default` (from secrets)
  - `JwtSettings__SecretKey` (from secrets)
  - `Supabase__Url` (from secrets)
  - `Supabase__AnonKey` (from secrets)
- [ ] Enable HTTPS only
- [ ] Configure custom domain (optional)
- [ ] Set up automatic SSL certificate (Let's Encrypt)

### Post-Deployment Verification
- [ ] Create deployment verification script or manual checklist:
  - Health check endpoint returns 200 OK
  - Database migrations applied successfully
  - Login/authentication works
  - API endpoints respond correctly
  - Blazor UI loads and navigates properly
  - Error pages configured

## Free Tier Limitations

- **CPU Time**: 60 minutes per day
- **Memory**: 1 GB
- **Storage**: 1 GB
- **Custom domain**: Not included (use azurewebsites.net)
- **Scaling**: Manual only, no auto-scale

## Monitoring

- [ ] Set up Azure Application Insights (free tier: 1 GB/month)
- [ ] Configure alerts for:
  - Application errors
  - High response times
  - Failed health checks
- [ ] Monitor resource usage to stay within free tier limits

## Testing

- Manual deployment test before automating
- Verify CD workflow triggers correctly
- Verify health checks work in production
- Load test to ensure free tier can handle expected traffic
- Verify database migrations run automatically on deployment

## Success Criteria

- Application is publicly accessible via Azure URL
- CD pipeline automatically deploys on main branch commits (after CI passes)
- Health checks report application status
- Production logging captures errors
- Database connection works from Azure
- Authentication works with Supabase
- All features work in production environment
- Deployment process is documented and repeatable
