# UserStory-16: Deploy to Hetzner VPS

## Objective

Deploy the Blazor Server application to a Hetzner VPS using Docker and Nginx,
with automated CD via GitHub Actions. Replaces the original Azure App Service plan
due to Blazor Server's permanent SignalR connection being incompatible with Azure
F1 free tier CPU limits and sleep behaviour.

## Architecture

```
GitHub Actions → Docker build → GitHub Container Registry (GHCR)
                                        ↓
Hetzner CX22 VPS → Docker container (.NET 10 Blazor Server)
                 → Nginx (HTTPS termination, reverse proxy)
                 → Let's Encrypt SSL (Certbot)
                 → Supabase (PostgreSQL, unchanged)
```

## Requirements

- Hetzner CX22 VPS (Ubuntu 24.04 LTS, 2 vCPU, 4 GB RAM, ~€3.79/mnd)
- Docker + Docker Compose on VPS
- Nginx as reverse proxy with HTTPS
- Let's Encrypt SSL via Certbot
- GitHub Actions CD pipeline (deploy on main after CI passes)
- Supabase PostgreSQL (existing, no changes needed)
- Health check endpoint (already registered in `Program.cs`)
- Production logging

## Why Not Azure F1

|                | Azure F1            | Hetzner CX22  |
| -------------- | ------------------- | ------------- |
| **Prijs**      | Gratis              | €3.79/mnd     |
| **Always-on**  | Nee (slaapt)        | Ja            |
| **SignalR**    | Verbreekt bij slaap | Stabiel       |
| **CPU limiet** | 60 min/dag          | Geen          |
| **Beheer**     | Managed             | Zelf (Docker) |

Azure F1 is fundamenteel ongeschikt voor Blazor Server vanwege de permanente
SignalR verbinding en de 60-minuten CPU dagslimiet.

## IPv4 vs IPv6

Start met **IPv6-only** (inbegrepen, geen extra kosten). Als er compatibiliteitsproblemen
optreden (ISP of Supabase), voeg IPv4 toe via Hetzner Cloud Console → Networking →
"Add IPv4" (~€0.72/mnd extra). Geen herstart of config-wijzigingen nodig op de VPS.

## Implementation Tasks

### Phase 1: Infrastructure Setup (eenmalig, handmatig)

#### 1.1 Hetzner VPS Provisioning

- [ ] **[User]** Create Hetzner account at [hetzner.com/cloud](https://www.hetzner.com/cloud)
- [ ] **[User]** Create new project (e.g. `localfinancemanager`)
- [ ] **[User]** Provision **CX22** server:
  - OS: **Ubuntu 24.04 LTS**
  - Location: Falkenstein (EU) of nearest region
  - SSH key: upload your public key (`~/.ssh/id_ed25519.pub`)
  - IPv6: included by default — IPv4 optional (+€0.72/mnd, addable later)
- [ ] **[User]** Note the assigned IP address (IPv6 or IPv4)

#### 1.1a SSH Key genereren (Windows, eenmalig) — **[User]**

Als je nog geen SSH key hebt:

```powershell
# Genereer een nieuwe Ed25519 key
ssh-keygen -t ed25519 -C "jouw@email.com"

# Accepteer de standaard locatie (C:\Users\<naam>\.ssh\id_ed25519)
# Stel optioneel een passphrase in

# Bekijk je publieke key — deze string kopieer je naar Hetzner
Get-Content "$env:USERPROFILE\.ssh\id_ed25519.pub"
```

Output ziet er zo uit:

```
ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAA... jouw@email.com
```

Upload deze publieke key in Hetzner Cloud Console bij het aanmaken van de VPS (sectie "SSH Keys").

#### 1.2 Initial Server Hardening — **[User]**

```bash
# SSH into the server
ssh root@<server-ip>

# Update packages
apt update && apt upgrade -y

# Create a non-root deploy user
adduser deploy
usermod -aG sudo deploy
usermod -aG docker deploy  # pre-emptive, docker installed next

# Copy SSH key for deploy user
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
chmod 600 /home/deploy/.ssh/authorized_keys

# Disable root SSH login and password authentication
sed -i 's/PermitRootLogin yes/PermitRootLogin no/' /etc/ssh/sshd_config
sed -i 's/#PasswordAuthentication yes/PasswordAuthentication no/' /etc/ssh/sshd_config
systemctl restart sshd
```

#### 1.3 Firewall (ufw) — **[User]**

```bash
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP (redirect to HTTPS)
ufw allow 443/tcp   # HTTPS
ufw enable
ufw status
```

#### 1.4 Docker installeren — **[User]**

```bash
# Install Docker via official script
curl -fsSL https://get.docker.com | sh

# Install Docker Compose plugin
apt install -y docker-compose-plugin

# Verify
docker --version
docker compose version
```

#### 1.5 Nginx installeren — **[User]**

```bash
apt install -y nginx
systemctl enable nginx
systemctl start nginx
```

#### 1.6 Domain instellen — **[User]**

- [ ] **[User]** Maak subdomain aan bij Vimexx: DNS Beheer → Record toevoegen → type `AAAA` (IPv6) of `A` (IPv4), naam bijv. `finance`, waarde = VPS IP
- [ ] **[User]** Wacht op DNS propagatie: `nslookup finance.<jouwdomein>` → moet VPS IP teruggeven
- [ ] **[User]** Verify Nginx is reachable: `curl http://finance.<jouwdomein>` → default Nginx page

#### 1.7 Let's Encrypt SSL via Certbot — **[User]**

```bash
# Install Certbot
apt install -y certbot python3-certbot-nginx

# Generate certificate (Nginx plugin handles config automatically)
certbot --nginx -d <your-domain>

# Verify auto-renewal
certbot renew --dry-run

# Certbot adds a cron job automatically; verify:
systemctl status certbot.timer
```

#### 1.8 App directory aanmaken — **[User]**

```bash
mkdir -p /opt/localfinancemanager
chown deploy:deploy /opt/localfinancemanager
```

### Phase 2: Application Configuration

- [ ] **[Copilot]** Create `appsettings.Production.json` with production log levels:
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
- [ ] **[User]** Verify health check endpoint responds at `/health` (already registered in `Program.cs`)

### Phase 3: Dockerfile

- [ ] **[Copilot]** Create `Dockerfile` in solution root:

  ```dockerfile
  FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
  WORKDIR /app
  EXPOSE 8080

  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  WORKDIR /src
  COPY . .
  RUN dotnet publish LocalFinanceManager/LocalFinanceManager.csproj -c Release -o /app/publish

  FROM base AS final
  WORKDIR /app
  COPY --from=build /app/publish .
  ENTRYPOINT ["dotnet", "LocalFinanceManager.dll"]
  ```

- [ ] **[Copilot]** Add `.dockerignore` to exclude `bin/`, `obj/`, `*.db`, `tests/`, `.git/`

### Phase 4: VPS Docker Compose

- [ ] **[Copilot]** Provide `docker-compose.yml` template:
  ```yaml
  services:
    app:
      image: ghcr.io/<owner>/localfinancemanager:latest
      restart: unless-stopped
      ports:
        - "127.0.0.1:8080:8080"
      environment:
        - ASPNETCORE_ENVIRONMENT=Production
        - ConnectionStrings__Local=${SUPABASE_CONNECTION_STRING}
        - Supabase__Url=${SUPABASE_URL}
        - Supabase__AnonKey=${SUPABASE_ANON_KEY}
  ```
- [ ] **[User]** Place `docker-compose.yml` at `/opt/localfinancemanager/docker-compose.yml` on the VPS
- [ ] **[User]** Create `/opt/localfinancemanager/.env` on VPS with production secrets (never commit to git)

### Phase 5: Nginx Configuration

- [ ] **[Copilot]** Provide Nginx config template:

  ```nginx
  server {
      listen 443 ssl;
      listen [::]:443 ssl;
      server_name <your-domain>;

      ssl_certificate /etc/letsencrypt/live/<your-domain>/fullchain.pem;
      ssl_certificate_key /etc/letsencrypt/live/<your-domain>/privkey.pem;

      location / {
          proxy_pass http://127.0.0.1:8080;
          proxy_http_version 1.1;
          proxy_set_header Upgrade $http_upgrade;
          proxy_set_header Connection "upgrade";  # Required for SignalR/WebSocket
          proxy_set_header Host $host;
          proxy_set_header X-Real-IP $remote_addr;
          proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
          proxy_set_header X-Forwarded-Proto $scheme;
          proxy_read_timeout 3600;  # Keep SignalR connections alive (1 hour)
      }
  }

  server {
      listen 80;
      listen [::]:80;
      server_name <your-domain>;
      return 301 https://$host$request_uri;
  }
  ```

- [ ] **[User]** Place config at `/etc/nginx/sites-available/localfinancemanager` on the VPS, replace `<your-domain>` with actual subdomain
- [ ] **[User]** Enable site: `ln -s /etc/nginx/sites-available/localfinancemanager /etc/nginx/sites-enabled/`
- [ ] **[User]** Test and reload: `nginx -t && systemctl reload nginx`

### Phase 6: GitHub Actions CD Pipeline

- [ ] **[Copilot]** Create `.github/workflows/deploy.yml`:

  ```yaml
  name: Deploy to Hetzner

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

        - name: Log in to GitHub Container Registry
          uses: docker/login-action@v3
          with:
            registry: ghcr.io
            username: ${{ github.actor }}
            password: ${{ secrets.GITHUB_TOKEN }}

        - name: Build and push Docker image
          uses: docker/build-push-action@v5
          with:
            context: .
            push: true
            tags: ghcr.io/${{ github.repository_owner }}/localfinancemanager:latest

        - name: Deploy to Hetzner via SSH
          uses: appleboy/ssh-action@v1
          with:
            host: ${{ secrets.HETZNER_HOST }}
            username: ${{ secrets.HETZNER_USER }}
            key: ${{ secrets.HETZNER_SSH_KEY }}
            script: |
              docker pull ghcr.io/${{ github.repository_owner }}/localfinancemanager:latest
              docker compose -f /opt/localfinancemanager/docker-compose.yml up -d
              docker image prune -f
  ```

### Phase 7: GitHub Secrets configureren — **[User]**

- [ ] **[User]** Add GitHub repository secrets (Settings → Secrets → Actions):
  - `HETZNER_HOST` — VPS IP address (IPv6 `[2a01:...]` of IPv4)
  - `HETZNER_USER` — SSH username (bijv. `deploy`)
  - `HETZNER_SSH_KEY` — private SSH key (`cat ~/.ssh/id_ed25519` — de **private** key)
  - `SUPABASE_CONNECTION_STRING` — PostgreSQL connection string
  - `SUPABASE_URL` — Supabase project URL
  - `SUPABASE_ANON_KEY` — Supabase anon/public key

### Phase 8: Post-Deployment Verification — **[User]**

- [ ] **[User]** Health check: `curl https://finance.<jouwdomein>/health` returns 200 OK
- [ ] **[User]** Database migrations applied on startup (automatic via `MigrateAsync`)
- [ ] **[User]** Login/authentication via Supabase works
- [ ] **[User]** SignalR/Blazor circuit connects (no WebSocket errors in browser console)
- [ ] **[User]** All pages load and navigate correctly
- [ ] **[User]** Certbot auto-renewal configured: `certbot renew --dry-run`

## Ownership Split

| Task                                       | Owner                     |
| ------------------------------------------ | ------------------------- |
| VPS provisioning, SSH setup, firewall      | **User** (Hetzner portal) |
| Domain registration                        | **User**                  |
| `.env` file with production secrets on VPS | **User**                  |
| Dockerfile, `.dockerignore`                | **Copilot**               |
| `appsettings.Production.json`              | **Copilot**               |
| `.github/workflows/deploy.yml`             | **Copilot**               |
| Nginx config template                      | **Copilot**               |
| GitHub Secrets configuration               | **User** (GitHub portal)  |

## Success Criteria

- Application is publicly accessible via HTTPS
- SignalR (Blazor Server circuit) stays connected without interruption
- CD pipeline automatically deploys on main branch commits after CI passes
- Health check at `/health` reports OK
- Supabase authentication works in production
- Database migrations run automatically on deployment
- Nginx correctly proxies WebSocket upgrades for SignalR
