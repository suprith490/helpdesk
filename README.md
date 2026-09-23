# HelpDesk - IT Support Ticket Management System

A full-stack HelpDesk / IT Support ticket tracking application built with
**ASP.NET Core 8 Web API**, **Entity Framework Core**, **SQL Server**, and a
plain **HTML/CSS/Vanilla JavaScript** frontend. It is designed as a
portfolio/resume project for a Junior .NET Software Engineer role.

## Features

- JWT authentication (register / login) with BCrypt password hashing
- Role-based authorization: **Employee**, **Support Agent**, **Admin**
- Ticket CRUD with generated ticket numbers (`HD-10001`, `HD-10002`, ...)
- Categories and departments
- Priority levels: Low / Medium / High / Critical
- Status workflow: New -> Assigned -> In Progress -> Resolved -> Closed
- Ticket assignment to support agents
- Public and internal comments
- Ticket history / audit log for every field change
- Search, filtering, sorting and pagination
- Role-aware dashboards with statistics
- Global exception handling and input validation
- Swagger / OpenAPI documentation
- xUnit unit tests (37 tests)

## Tech Stack

| Layer      | Technology                                          |
|------------|-----------------------------------------------------|
| Backend    | C#, ASP.NET Core 8 Web API                          |
| Data       | Entity Framework Core 8, SQL Server (SQLite in dev) |
| Auth       | JWT Bearer, BCrypt.Net-Next                         |
| API Docs   | Swashbuckle (Swagger UI)                            |
| Frontend   | HTML5, CSS3, Vanilla JavaScript (Fetch API)         |
| Tests      | xUnit, EF Core SQLite in-memory                     |
| Tooling    | .NET CLI, Git/GitHub, AWS-ready configuration       |

## Architecture

Clean, layered architecture. Dependencies point inward:

```
Controllers -> Services -> EF Core -> SQL Server
```

```
HelpDesk.sln
src/
  HelpDesk.Domain/          # Entities, enums, workflow rules (no dependencies)
  HelpDesk.Application/     # DTOs, service interfaces, common types
  HelpDesk.Infrastructure/  # EF Core DbContext, migrations, services, security
  HelpDesk.Api/             # Controllers, middleware, Program.cs, wwwroot
tests/
  HelpDesk.Tests/           # xUnit tests
docs/                       # Phase guide, comparisons, deployment, interview prep
```

This mirrors the Spring Boot idea of `entity` / `service` / `repository`
layers, except .NET uses interfaces + Dependency Injection registered in
`Program.cs` (instead of `@Service`, `@Repository`, `@Autowired`).

## Quick Start

### Prerequisites

- .NET 8 SDK (`dotnet --version` should show `8.x`)
- SQL Server (optional for local development)

### Run locally with SQLite (fastest, no SQL Server needed)

```bash
# Restore dependencies
dotnet restore

# Build everything
dotnet build

# Run the API (Development uses SQLite + a dev JWT key)
dotnet run --project src/HelpDesk.Api
```

Then open:

- Frontend: http://localhost:5080
- Swagger: http://localhost:5080/swagger

### Run with SQL Server

Edit `src/HelpDesk.Api/appsettings.Development.json` and set:

```json
{
  "Database": { "Provider": "SqlServer" },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Then apply migrations and run:

```bash
dotnet ef database update --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.Api
dotnet run --project src/HelpDesk.Api
```

### Run the tests

```bash
dotnet test
```

## Seeded Accounts

| Role      | Email                     | Password   |
|-----------|---------------------------|------------|
| Admin     | admin@helpdesk.local      | Admin@123  |

Register additional users through the UI or `POST /api/auth/register`; they
start as **Employee**. An Admin can promote them to **SupportAgent** from the
Users page or via `PUT /api/users/{id}`.

## API Overview

| Method | Endpoint                                   | Access                  |
|--------|--------------------------------------------|-------------------------|
| POST   | `/api/auth/register`                       | Anonymous               |
| POST   | `/api/auth/login`                          | Anonymous               |
| GET    | `/api/auth/me`                             | Authenticated           |
| GET    | `/api/tickets` (search/filter/sort/page)   | Authenticated (scoped)  |
| POST   | `/api/tickets`                             | Authenticated           |
| GET    | `/api/tickets/{id}`                        | Owner / Agent / Admin   |
| GET    | `/api/tickets/{id}/details`                | Owner / Agent / Admin   |
| GET    | `/api/tickets/{id}/history`                | Owner / Agent / Admin   |
| PUT    | `/api/tickets/{id}`                        | Owner / Agent / Admin   |
| POST   | `/api/tickets/{id}/assign`                 | Agent / Admin           |
| POST   | `/api/tickets/{id}/status`                 | Agent / Admin           |
| DELETE | `/api/tickets/{id}`                        | Admin                   |
| GET    | `/api/tickets/{ticketId}/comments`         | Owner / Agent / Admin   |
| POST   | `/api/tickets/{ticketId}/comments`         | Owner / Agent / Admin   |
| DELETE | `/api/tickets/{ticketId}/comments/{commentId}` | Owner / Admin         |
| GET    | `/api/categories`                          | Authenticated           |
| POST   | `/api/categories`                          | Admin                   |
| PUT    | `/api/categories/{id}`                     | Admin                   |
| GET    | `/api/users`                               | Admin                   |
| GET    | `/api/users/agents`                        | Agent / Admin           |
| GET    | `/api/users/me` / `PUT /api/users/me`      | Authenticated           |
| POST   | `/api/users/me/change-password`            | Authenticated           |
| PUT    | `/api/users/{id}`                          | Admin                   |
| GET    | `/api/dashboard`                           | Authenticated           |
| GET    | `/api/departments`                         | Anonymous               |
| GET    | `/api/health`, `/api/health/db`            | Anonymous               |

## Documentation

- `docs/PHASES.md` - the 16 build phases with commands and explanations
- `docs/SPRING-BOOT-COMPARISON.md` - .NET concepts mapped to Spring Boot
- `docs/AWS-DEPLOYMENT.md` - how to deploy the API to AWS
- `docs/DEPLOY-VERCEL-RENDER.md` - deploy the frontend to Vercel and the API to Render
- `docs/RESUME-AND-INTERVIEW.md` - resume bullets and interview Q&A
- `docs/ER-DIAGRAM.md` - database schema and indexes

## Deployment

The API is a .NET app, so it cannot run on Vercel directly. The recommended
free setup is a split deployment:

- **Frontend** (`src/HelpDesk.Api/wwwroot`) -> **Vercel** (static hosting)
- **API** (`HelpDesk.Api`) -> **Render** (Docker web service)
- Vercel rewrites `/api/*` to the Render API, so the browser uses one origin
  and no CORS configuration is needed.

Config files included: `Dockerfile`, `render.yaml`, `vercel.json`. Full
step-by-step instructions are in `docs/DEPLOY-VERCEL-RENDER.md`.

## Common Errors

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Jwt:Key must be configured and at least 32 characters long` | Missing/short `Jwt:Key` | Set a 32+ char key in `appsettings.Development.json` |
| `Cannot open database ... Login failed` | SQL Server not running | Use SQLite (`"Provider": "Sqlite"`) or start SQL Server |
| `Unable to create a 'DbContext'` | EF Core design package missing | Ensure `Microsoft.EntityFrameworkCore.Design` is referenced |
| CORS error in browser | Frontend served from another origin | Use the API's `wwwroot`, or add your origin to `Cors:AllowedOrigins` |
| Port 5080 already in use | Another instance running | Change `applicationUrl` in `launchSettings.json` |
