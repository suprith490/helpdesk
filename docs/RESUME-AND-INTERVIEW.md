# Resume & Interview Preparation

## 1. Resume entry (drop-in)

**HelpDesk - IT Support Ticket Management System** | *ASP.NET Core 8, EF Core, SQL Server, JWT, Vanilla JS*

- Built a full-stack ticket management system using ASP.NET Core 8 Web API,
  Entity Framework Core, and SQL Server with a layered
  Controllers -> Services -> EF Core architecture.
- Implemented JWT authentication with BCrypt password hashing and
  role-based authorization for Employee, Support Agent, and Admin roles.
- Designed 6 relational entities (Users, Departments, Categories, Tickets,
  Comments, TicketHistory) with foreign keys, unique indexes, and EF Core
  migrations.
- Developed a validated status workflow (New -> Assigned -> In Progress ->
  Resolved -> Closed), ticket assignment, internal/public comments, and a
  per-field audit log.
- Added server-side search, filtering, sorting, and pagination, plus
  role-aware dashboards with aggregate statistics.
- Wrote 37 xUnit tests using an in-memory SQLite database and test doubles;
  documented the API with Swagger/OpenAPI and prepared an AWS deployment path
  (App Runner + RDS SQL Server).

Short version for a one-page resume:

> **HelpDesk Ticket System** - ASP.NET Core 8, EF Core, SQL Server, JWT, REST,
> xUnit. Full-stack, role-based IT support portal with ticket workflow, audit
> history, dashboards, and 37 unit tests.

## 2. The 60-second project pitch

> "I built a HelpDesk ticket management system to practise backend
> engineering end to end. It has three roles - employees raise tickets,
> agents assign and resolve them, admins manage users and categories. The
> backend is ASP.NET Core with a clean layered architecture: controllers are
> thin, business logic lives in services, and EF Core handles persistence.
> Authentication is JWT-based with BCrypt password hashing and role-based
> authorization. I enforced the ticket lifecycle with a status workflow,
> recorded every change in an audit table, and added search, filtering,
> pagination, and dashboards. I tested the service layer with xUnit and an
> in-memory SQLite database. It's deployed-ready for AWS with configuration
> supplied through environment variables."

## 3. Architecture questions

**Q: Walk me through the architecture.**

Controllers -> Services -> EF Core -> SQL Server. `HelpDesk.Domain` holds
entities and workflow rules and has no dependencies. `HelpDesk.Application`
holds DTOs and service interfaces. `HelpDesk.Infrastructure` implements the
interfaces, the `DbContext`, migrations, and security. `HelpDesk.Api` is the
composition root. Dependencies point inward, so the domain never references a
framework.

**Q: Why DTOs instead of returning entities?**

To avoid over-posting, leaking fields like `PasswordHash`, and coupling the API
contract to the database schema. DTOs also let me shape responses (for example
`CreatedByName`) without exposing navigation graphs.

**Q: Why an interface for every service?**

It keeps controllers decoupled from implementations and makes the services easy
to fake in tests. It is the same motivation as coding to interfaces in Spring.

## 4. EF Core & database questions

**Q: Code-first or database-first?**

Code-first. I define entities and fluent configurations, then generate
migrations with `dotnet ef migrations add`. This is the EF Core analogue of
Flyway/Liquibase, but the scripts are generated from the model.

**Q: How did you model relationships?**

`User -> Department` many-to-one, `Ticket -> Category` many-to-one,
`Ticket -> User` twice (creator and assignee), `Ticket -> Comments` and
`Ticket -> History` one-to-many. I set delete behaviour to `Restrict` for
users/categories and `SetNull` for the assignee.

**Q: `AsNoTracking` - what and why?**

For read-only queries it tells EF Core not to track entities in the change
tracker, reducing memory and CPU. I use it in list/detail reads.

**Q: How do you prevent N+1 queries?**

Eager loading with `Include` / `ThenInclude`, and projecting straight to DTOs.

**Q: How did you generate `HD-10001`?**

Inside a transaction: insert the ticket to get its identity Id, then set
`TicketNumber = $"HD-{10000 + Id}"` and save again. The number column is
unique-indexed.

## 5. Authentication & security questions

**Q: How does JWT auth work here?**

On login the API verifies the BCrypt hash, then issues a signed JWT containing
the user id and role. The client stores it and sends `Authorization: Bearer
<token>`. The API validates issuer, audience, signature, and lifetime on every
request.

**Q: Why BCrypt and not SHA-256?**

BCrypt is purpose-built for passwords: it salts automatically and is
deliberately slow, which resists brute force. Plain SHA is fast and vulnerable
to rainbow tables.

**Q: How is authorization enforced?**

`[Authorize]` at the controller and `[Authorize(Roles = "Admin")]` for specific
actions, plus service-level checks (for example employees can only read their
own tickets). Defence in depth: even if a route is misconfigured, the service
checks ownership.

**Q: How do you avoid SQL injection?**

EF Core parameterises LINQ queries. I never concatenate raw SQL with user
input.

## 6. C# / .NET questions

**Q: DI lifetimes?**

`AddScoped` for `DbContext` and services (one instance per HTTP request),
`AddSingleton` for stateless singletons, `AddTransient` for short-lived
objects. A `DbContext` must never be a singleton because it is not thread-safe.

**Q: `async`/`await` - why?**

Database and network I/O should not block a thread. `await _db.Tickets.ToListAsync()`
frees the thread while waiting, which improves throughput under load.

**Q: What does `[ApiController]` give you?**

Automatic model validation (400 on invalid DataAnnotations), binding source
inference, and problem-details error responses.

**Q: How is global exception handling done?**

A custom `IExceptionHandler` maps typed exceptions (`NotFoundException`,
`ForbiddenException`, ...) to RFC 7807 `ProblemDetails` with the right status
code, so controllers stay clean.

## 7. Testing questions

**Q: How do you test the service layer?**

xUnit with an EF Core **SQLite in-memory** database, plus small fakes for
`ICurrentUserService` and `IJwtTokenService`. Each test gets a fresh database,
so tests are isolated and fast (no SQL Server required).

**Q: Why SQLite instead of the InMemory provider?**

The InMemory provider does not enforce relational constraints (unique indexes,
foreign keys), so it would not catch the bugs that matter. SQLite is a real
relational engine.

**Q: What did you test?**

Status transition rules, password hashing, registration/login edge cases
(duplicate email, wrong password, deactivated account), ticket number
generation, role-based visibility, assignment, workflow, internal comment
hiding, and pagination math.

## 8. Behavioural / design questions

**Q: What was the hardest part?**

The status workflow plus audit log: keeping the transition map in the domain,
validating it in the service, and writing a history row atomically in the same
transaction.

**Q: What would you add next?**

Refresh tokens, email notifications, attachments, a CI pipeline with build+test
on every push, and integration tests with `WebApplicationFactory`.

**Q: How would you scale it?**

Read replicas / caching for dashboards, background jobs for notifications, and
splitting the API from the frontend static hosting (S3 + CloudFront).

## 9. Quick revision checklist

- [ ] Explain the layering and why dependencies point inward
- [ ] Draw the ER diagram from memory
- [ ] Explain JWT issuance and validation
- [ ] Explain DI lifetimes, especially why `DbContext` is scoped
- [ ] Describe the status workflow and an illegal transition
- [ ] Describe how pagination works (`Skip`/`Take` + total count)
- [ ] Explain `AsNoTracking` and eager loading
- [ ] Describe the test setup and one interesting test
- [ ] Give the AWS deployment story in three sentences
