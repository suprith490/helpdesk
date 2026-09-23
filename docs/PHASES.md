# Build Phases Guide

This document walks through the 16 phases used to build the HelpDesk system.
Every phase lists the goal, the exact commands, the files involved, the Spring
Boot equivalent, and common errors.

> Note: All commands assume you are in the repository root (`/workspace`).

---

## Phase 1 - Setup

**Goal:** install the toolchain and create the solution skeleton.

```bash
# Check the SDK
dotnet --version

# Create the solution
dotnet new sln -n HelpDesk

# Create the projects
dotnet new classlib -n HelpDesk.Domain        -o src/HelpDesk.Domain
dotnet new classlib -n HelpDesk.Application   -o src/HelpDesk.Application
dotnet new classlib -n HelpDesk.Infrastructure -o src/HelpDesk.Infrastructure
dotnet new webapi  -n HelpDesk.Api            -o src/HelpDesk.Api
dotnet new xunit   -n HelpDesk.Tests          -o tests/HelpDesk.Tests

# Add them to the solution
dotnet sln add src/HelpDesk.Domain src/HelpDesk.Application src/HelpDesk.Infrastructure src/HelpDesk.Api tests/HelpDesk.Tests

# Wire up project references (Domain <- Application <- Infrastructure <- Api)
dotnet add src/HelpDesk.Application reference src/HelpDesk.Domain
dotnet add src/HelpDesk.Infrastructure reference src/HelpDesk.Application src/HelpDesk.Domain
dotnet add src/HelpDesk.Api reference src/HelpDesk.Application src/HelpDesk.Infrastructure
dotnet add tests/HelpDesk.Tests reference src/HelpDesk.Domain src/HelpDesk.Application src/HelpDesk.Infrastructure

# Install backend packages
dotnet add src/HelpDesk.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/HelpDesk.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/HelpDesk.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/HelpDesk.Api package Swashbuckle.AspNetCore
dotnet add src/HelpDesk.Infrastructure package BCrypt.Net-Next
```

**Spring Boot equivalent:** `spring init --dependencies=web,data-jpa,security`
or the Spring Initializr. Maven modules map to .NET projects in a solution.

**Common errors**
- `dotnet: command not found` -> install the .NET 8 SDK.
- Package restore 401/403 -> corporate proxy; configure a NuGet source.

---

## Phase 2 - ASP.NET Core project

**Goal:** configure the web host and middleware pipeline.

Key file: `src/HelpDesk.Api/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ... JWT, CORS, DI, exception handling ...

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

`Program.cs` is the **composition root**. `builder.Services` is the DI
container; `app.Use...` is the middleware pipeline.

**Spring Boot equivalent**

| ASP.NET Core                  | Spring Boot                        |
|-------------------------------|------------------------------------|
| `Program.cs` / `Startup.cs`   | `@SpringBootApplication` main class|
| `builder.Services.AddScoped`  | `@Bean` / stereotype annotations   |
| `app.UseAuthentication()`     | `SecurityFilterChain`              |
| Middleware                    | Servlet `Filter` / `HandlerInterceptor` |

**Common errors**
- Middleware order matters: `UseAuthentication()` **must** come before
  `UseAuthorization()`.
- Minimal API vs Controllers: this project uses controllers.

---

## Phase 3 - SQL Server + EF Core

**Goal:** connect EF Core to the database.

Packages live in `src/HelpDesk.Infrastructure/HelpDesk.Infrastructure.csproj`.
Registration lives in `DependencyInjection.cs`:

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    if (provider == "Sqlite")
        options.UseSqlite(configuration.GetConnectionString("SqliteConnection"));
    else
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
});
```

Create the first migration and apply it:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/HelpDesk.Infrastructure \
  --startup-project src/HelpDesk.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/HelpDesk.Infrastructure \
  --startup-project src/HelpDesk.Api
```

**Spring Boot equivalent:** `spring.jpa.*` in `application.properties`,
Flyway/Liquibase for migrations, and `@Transactional`. EF migrations are the
direct analogue of Flyway versioned scripts but generated from the model.

**Common errors**
- `Unable to create a 'DbContext' of type ...` -> add `Microsoft.EntityFrameworkCore.Design`.
- `dotnet ef` not found -> `dotnet tool install --global dotnet-ef` then reopen the shell.

---

## Phase 4 - Models & relationships

**Goal:** model the domain and map it fluently.

Entities (`src/HelpDesk.Domain/Entities/`):

- `User` (Id, names, Email, PasswordHash, Role, DepartmentId, IsActive)
- `Department` (Id, Name) - one-to-many with `User`
- `Category` (Id, Name, IsActive) - one-to-many with `Ticket`
- `Ticket` (TicketNumber, Title, CategoryId, CreatedById, AssignedToId, Priority, Status, timestamps)
- `Comment` (TicketId, UserId, Body, IsInternal)
- `TicketHistory` (TicketId, ChangedById, FieldName, OldValue, NewValue)

Fluent configuration (`src/HelpDesk.Infrastructure/Persistence/Configurations/`)
defines keys, max lengths, unique indexes (`Email`, `TicketNumber`,
`Category.Name`), and delete behaviour (`Restrict` for users/categories,
`SetNull` for assignee).

```bash
# After editing entity configs, add a migration
dotnet ef migrations add AddTicketIndexes \
  --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.Api
```

**Spring Boot equivalent:** JPA `@Entity`, `@ManyToOne`, `@OneToMany`, and
`@Column(unique = true)`. EF "shadow" behaviour is explicit here.

**Common errors**
- Missing unique index -> duplicate ticket numbers under load.
- `DeleteBehavior` cycle -> use `Restrict`/`SetNull` as done here.

---

## Phase 5 - Authentication + JWT

**Goal:** issue and validate JWTs, hash passwords.

Key files:
- `Infrastructure/Security/PasswordHasher.cs` (BCrypt)
- `Infrastructure/Security/JwtTokenService.cs` (token creation)
- `Infrastructure/Security/JwtSettings.cs` (Issuer/Audience/Key/Expiry)
- `Infrastructure/Services/AuthService.cs` (register/login)
- `Api/Controllers/AuthController.cs`

Configuration (`appsettings.Development.json`):

```json
{ "Jwt": { "Key": "dev-only-signing-key-change-me-please-0123456789abcdef" } }
```

**Spring Boot equivalent:** Spring Security + `io.jsonwebtoken` (jjwt). The
`JwtAuthenticationFilter` maps to `AddJwtBearer(...)` in
`Program.cs`; `BCryptPasswordEncoder` maps to `BCrypt.Net-Next`.

**Common errors**
- `IDX10653: key too short` -> HMAC-SHA256 needs >= 32 bytes.
- Users get instant 401 after restart -> `ValidateLifetime` + clock skew; the
  dev key must stay stable.

---

## Phase 6 - Ticket APIs

**Goal:** CRUD endpoints for tickets.

`Api/Controllers/TicketsController.cs` delegates to
`Infrastructure/Services/TicketService.cs`.

```bash
# Example: create a ticket
curl -X POST http://localhost:5080/api/tickets \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Laptop will not boot","description":"Black screen after login.","categoryId":1,"priority":"High"}'
```

Ticket numbers are generated as `HD-{10000 + Id}` inside a transaction, giving
`HD-10001`, `HD-10002`, ...

**Spring Boot equivalent:** `@RestController` + `@Service` + `@Repository`.
ASP.NET `IActionResult` maps to `ResponseEntity`.

---

## Phase 7 - Assignment & status workflow

**Goal:** enforce legal status transitions and assignment rules.

`src/HelpDesk.Domain/Workflow/TicketStatusWorkflow.cs`:

```text
New -> Assigned -> In Progress -> Resolved -> Closed
```

Transitions are validated in `TicketService.ChangeStatusAsync`, which throws
`BadRequestException` (HTTP 400) for illegal moves and writes a history row.

**Spring Boot equivalent:** a `StateMachine` or a domain service with a
transition map - exactly what this class is.

---

## Phase 8 - Comments & history

**Goal:** discussion threads plus a full audit log.

- Comments: `Api/Controllers/CommentsController.cs` and `CommentService`.
  `IsInternal = true` comments are hidden from Employees.
- History: every field change appends a `TicketHistory` row with
  `FieldName`, `OldValue`, `NewValue`, `ChangedById`.

**Spring Boot equivalent:** JPA `@PrePersist`/`@PreUpdate` listeners or Spring
Data's `@LastModifiedBy` auditing (`AuditingEntityListener`).

---

## Phase 9 - Search, filtering, pagination

**Goal:** server-side querying with `IQueryable`.

`GET /api/tickets?search=vpn&status=New&priority=High&categoryId=1&sortBy=priority&sortDirection=desc&page=1&pageSize=10`

`TicketService.GetAllAsync` composes `Where`, `OrderBy`, `Skip`, `Take`, and
returns a `PagedResult<T>` with `TotalCount` and `TotalPages`.

**Spring Boot equivalent:** Spring Data `Pageable` + `Specification`. Here we
build the `IQueryable` predicate manually.

---

## Phase 10 - Dashboards

**Goal:** role-aware statistics.

`GET /api/dashboard` returns counts by status/priority/category, user stats
(admin only), and recent tickets. Employees only see their own numbers; agents
and admins see the whole system. See
`Application/DTOs/Dashboard/DashboardResponse.cs`.

---

## Phase 11 - HTML/CSS/JS frontend

**Goal:** a responsive SPA-ish frontend served from `wwwroot`.

Pages: `login`, `register`, `dashboard`, `tickets`, `ticket-create`,
`ticket-details`, `categories`, `users`, `profile`.

`wwwroot/js/api.js` centralises `fetch("/api" + path)` and attaches the JWT
from `localStorage`. Because the frontend is same-origin with the API there
are no CORS issues.

```bash
dotnet run --project src/HelpDesk.Api
# open http://localhost:5080
```

**Spring Boot equivalent:** static resources in `src/main/resources/static`.

**Common errors**
- 401 everywhere -> token missing/expired; `api.js` clears storage on 401.
- Blank page -> open the browser dev console; check `/api/health`.

---

## Phase 12 - Testing

**Goal:** fast, isolated unit tests.

```bash
dotnet test
```

`tests/HelpDesk.Tests/` contains:

- `Domain/TicketStatusWorkflowTests.cs` - transition matrix
- `Security/PasswordHasherTests.cs` - BCrypt round-trip
- `Services/AuthServiceTests.cs` - register/login/duplicate/deactivated
- `Services/TicketServiceTests.cs` - numbering, scoping, assignment, workflow
- `Common/PagedResultTests.cs` - paging math
- `TestSupport/` - SQLite in-memory database, fake current user, fake JWT, data factory

**Spring Boot equivalent:** JUnit 5 + `@DataJpaTest` / `@SpringBootTest`,
Mockito for collaborators.

**Common errors**
- Tests bleeding into each other -> the SQLite `:memory:` connection must stay
  open and pooling disabled (see `SqliteTestDatabase.cs`).
- Seeded rows (`HasData`) cause unique collisions -> reuse them in test data.

---

## Phase 13 - Swagger

**Goal:** interactive API docs with JWT support.

`Program.cs` registers a `Bearer` security scheme. Run the API and open:

```text
http://localhost:5080/swagger
```

Click **Authorize**, paste the token from `/api/auth/login`, and call secured
endpoints.

**Spring Boot equivalent:** springdoc-openapi (`/swagger-ui.html`).

---

## Phase 14 - GitHub

**Goal:** version control the project.

```bash
git init
git add .
git commit -m "feat: complete HelpDesk ticket management system"
git branch -M main
git remote add origin https://github.com/<you>/helpdesk.git
git push -u origin main
```

Use a `.gitignore` that excludes `bin/`, `obj/`, `*.db`, and user secrets.

---

## Phase 15 - AWS deployment

See `docs/AWS-DEPLOYMENT.md`. In short: publish a self-contained build, run it
under Elastic Beanstalk / ECS / App Runner, point it at RDS SQL Server, and
inject `ConnectionStrings__DefaultConnection` and `Jwt__Key` as environment
variables.

```bash
dotnet publish src/HelpDesk.Api -c Release -o publish
```

**Spring Boot equivalent:** `mvn package` + `java -jar`, deployed to the same
AWS compute services.

---

## Phase 16 - Resume & interview preparation

See `docs/RESUME-AND-INTERVIEW.md` for resume bullets, a project pitch, and
common interview questions with model answers.
