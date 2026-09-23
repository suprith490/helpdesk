# AWS Deployment Guide

This guide deploys the HelpDesk API plus its static frontend to AWS and points
it at a managed SQL Server database. It stays deliberately simple - no
Kubernetes, no Kafka. The two realistic options are **AWS App Runner** (least
ops) and **Elastic Beanstalk** (classic).

## 0. What we are deploying

- `HelpDesk.Api` serves both the REST API (`/api/*`) and the frontend
  (`wwwroot/*`) from a single process and port.
- The database becomes **Amazon RDS for SQL Server**.
- Configuration (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, ...) is
  injected as environment variables, never committed to the repo.

## 1. Prepare the production publish

```bash
# From the repository root
dotnet publish src/HelpDesk.Api -c Release -o publish
```

The `publish` folder contains `HelpDesk.Api.dll` and everything it needs.

## 2. Add a Dockerfile (recommended for App Runner / ECS)

Create a file named `Dockerfile` in the repository root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/HelpDesk.Api/HelpDesk.Api.csproj
RUN dotnet publish src/HelpDesk.Api/HelpDesk.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HelpDesk.Api.dll"]
```

Build and test the image locally:

```bash
docker build -t helpdesk-api .
docker run -p 8080:8080 \
  -e Database__Provider=Sqlite \
  -e Jwt__Key=local-test-key-at-least-32-characters-long \
  helpdesk-api
```

## 3. Create the database (RDS SQL Server)

1. In the AWS console open **RDS -> Create database -> SQL Server**.
2. Choose the free-tier-eligible size for learning.
3. Put it in a security group that allows port `1433` from your compute
   service (not from `0.0.0.0/0`).
4. Note the endpoint, master username, and password.

Production connection string:

```text
Server=helpdesk.<id>.<region>.rds.amazonaws.com,1433;Database=HelpDeskDb;
User Id=admin;Password=<strong-password>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Apply migrations against RDS once (from a machine that can reach it):

```bash
export ConnectionStrings__DefaultConnection="Server=...;Database=HelpDeskDb;..."
dotnet ef database update \
  --project src/HelpDesk.Infrastructure \
  --startup-project src/HelpDesk.Api
```

Alternatively, let the app migrate on startup: `DatabaseInitializer` calls
`MigrateAsync()` for SQL Server automatically (SQLite uses `EnsureCreated`).

## 4A. Deploy with AWS App Runner (easiest)

1. Push the repository to GitHub.
2. **App Runner -> Create service -> Source: GitHub**.
3. Let App Runner build from the `Dockerfile`.
4. Port: `8080`.
5. Add environment variables (see section 5).
6. Deploy. App Runner gives you an HTTPS URL.

## 4B. Deploy with Elastic Beanstalk (alternative)

```bash
dotnet publish src/HelpDesk.Api -c Release -o publish

# From the publish folder, zip the output
cd publish
zip -r ../helpdesk-api.zip .
cd ..

# Deploy with the EB CLI
eb init helpdesk-api --platform "64bit Amazon Linux 2 v2.x running .NET Core"
eb create helpdesk-prod
eb deploy
eb setenv \
  ASPNETCORE_ENVIRONMENT=Production \
  Database__Provider=SqlServer \
  "ConnectionStrings__DefaultConnection=<rds-connection-string>" \
  "Jwt__Key=<32-plus-character-production-key>" \
  "Cors__AllowedOrigins__0=https://your-frontend-domain"
```

## 5. Required environment variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | Which settings file to load | `Production` |
| `ASPNETCORE_URLS` | Listen address | `http://+:8080` |
| `Database__Provider` | `SqlServer` or `Sqlite` | `SqlServer` |
| `ConnectionStrings__DefaultConnection` | RDS connection string | see above |
| `Jwt__Key` | Signing key, >= 32 chars | random secret |
| `Jwt__Issuer` / `Jwt__Audience` | Token validation | `HelpDesk` / `HelpDeskClient` |
| `Cors__AllowedOrigins__0` | Allowed browser origin | `https://app.example.com` |

> Double underscores (`__`) map to the `:` hierarchy in `appsettings.json`.

## 6. Store secrets safely

For App Runner / ECS, reference **AWS Secrets Manager** or **SSM Parameter
Store** instead of plaintext environment variables. In code, read them through
configuration; the source stays free of credentials.

```bash
aws secretsmanager create-secret \
  --name helpdesk/jwt-key \
  --secret-string "<32-plus-character-key>"
```

## 7. Post-deployment checklist

- [ ] `GET /api/health` returns `healthy`
- [ ] `GET /api/health/db` returns `database: reachable`
- [ ] Swagger is disabled or protected in Production (it is Development-only here)
- [ ] HTTPS is enabled and HTTP redirects to HTTPS
- [ ] The default admin password from `Seed:AdminPassword` has been changed
- [ ] RDS backups and a security group restricting port 1433 are enabled
- [ ] CloudWatch logging/alarms are enabled for the service

## 8. Cost & scope notes

This is a learning/portfolio deployment. For a resume you do **not** need a
multi-AZ, autoscaling production cluster. A single small App Runner instance
plus a free-tier RDS instance is enough to demonstrate "AWS-ready deployment".

## Common errors

| Symptom | Fix |
|---------|-----|
| App starts then exits | Check logs; usually a missing `Jwt__Key` or bad connection string |
| `Login failed for user` | Wrong RDS password or the user lacks access to the database |
| Timeout connecting to RDS | Security group does not allow port 1433 from the compute service |
| 500 on first request | Migrations not applied; run `database update` or let startup migrate |
