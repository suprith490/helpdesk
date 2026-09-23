# .NET vs Spring Boot - Concept Map

If you already know Java/Spring Boot, this table maps each concept to its
ASP.NET Core equivalent, followed by side-by-side code snippets.

## At a glance

| Spring Boot / Java              | ASP.NET Core / C#                         |
|---------------------------------|-------------------------------------------|
| `@SpringBootApplication`        | `Program.cs` (top-level statements)       |
| `@RestController`               | `[ApiController]` + `ControllerBase`      |
| `@Service`                      | service class + `AddScoped<IFoo, Foo>()`  |
| `@Repository`                   | EF Core `DbContext` / `DbSet<T>`          |
| `@Entity` / JPA                 | POCO entity + `IEntityTypeConfiguration`  |
| `@Autowired` / constructor      | Constructor injection (built-in DI)       |
| `@Bean`                         | `builder.Services.Add...`                 |
| `application.properties`        | `appsettings.json`                        |
| `@ConfigurationProperties`      | `IOptions<T>` + `Configure<T>()`          |
| `@Valid` / Bean Validation      | DataAnnotations + `[ApiController]`       |
| `ResponseEntity<T>`             | `IActionResult` / `ActionResult<T>`       |
| `@ControllerAdvice`             | `IExceptionHandler` / middleware          |
| Spring Security filter chain    | Authentication/Authorization middleware   |
| BCryptPasswordEncoder           | `BCrypt.Net-Next`                         |
| JUnit 5 + Mockito               | xUnit + hand-written test doubles         |
| Flyway / Liquibase              | EF Core Migrations                        |
| `Pageable` / `Page<T>`          | `Skip`/`Take` + `PagedResult<T>`          |
| Maven/Gradle                    | MSBuild + NuGet + `dotnet` CLI            |
| `mvn spring-boot:run`           | `dotnet run`                              |
| `mvn test`                      | `dotnet test`                             |

## 1. Application entry point

Spring Boot:

```java
@SpringBootApplication
public class HelpDeskApplication {
    public static void main(String[] args) {
        SpringApplication.run(HelpDeskApplication.class, args);
    }
}
```

ASP.NET Core (`src/HelpDesk.Api/Program.cs`):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<ITicketService, TicketService>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

There is no annotation-based component scan. You register every service
explicitly; `DependencyInjection.cs` in the Infrastructure project groups them.

## 2. Dependency injection

Spring Boot:

```java
@Service
public class TicketService {
    private final TicketRepository repository;

    @Autowired
    public TicketService(TicketRepository repository) {
        this.repository = repository;
    }
}
```

ASP.NET Core:

```csharp
public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }
}

// Registration
services.AddScoped<ITicketService, TicketService>();
```

`AddScoped` is the default per-request lifetime (like Spring's default
singleton is **not** the same - be careful). Use `AddSingleton` for stateless
singletons, `AddTransient` for short-lived objects.

## 3. Data access

Spring Data JPA:

```java
public interface TicketRepository extends JpaRepository<Ticket, Long> {
    List<Ticket> findByStatus(TicketStatus status);
}
```

EF Core:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();
}

// Querying
var tickets = await _db.Tickets
    .Where(t => t.Status == TicketStatus.New)
    .ToListAsync();
```

`DbSet<T>` is the analogue of a `JpaRepository`. There is no method-name query
derivation; you use LINQ (`Where`, `OrderBy`, `Include`) which EF translates to
SQL.

## 4. Transactions

Spring Boot: `@Transactional`.

EF Core:

```csharp
await using var transaction = await _db.Database.BeginTransactionAsync();
_db.Tickets.Add(ticket);
await _db.SaveChangesAsync();
await transaction.CommitAsync();
```

## 5. Validation

Spring Boot: `@Valid`, `@NotNull`, `@Size`.

ASP.NET Core: DataAnnotations on DTOs, enforced automatically because
controllers are decorated with `[ApiController]`.

```csharp
public class CreateTicketRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MinLength(10)]
    public string Description { get; set; } = string.Empty;
}
```

## 6. Error handling

Spring Boot: `@ControllerAdvice` + `@ExceptionHandler`.

ASP.NET Core: a custom `IExceptionHandler`
(`src/HelpDesk.Api/Middleware/GlobalExceptionHandler.cs`) plus typed exceptions
in `Application/Exceptions/ApiException.cs`. Domain code throws
`NotFoundException`, `BadRequestException`, etc., and one handler maps them to
RFC 7807 `ProblemDetails` responses.

## 7. Configuration & secrets

Spring Boot: `application.yml`, environment variables, Vault.

ASP.NET Core: `appsettings.json` layered with
`appsettings.{Environment}.json`, environment variables, and user secrets.
Nested keys use `__` as the separator: `Jwt__Key`, `ConnectionStrings__DefaultConnection`.

## 8. Testing

Spring Boot: JUnit 5, `@DataJpaTest`, `@SpringBootTest`.

ASP.NET Core: xUnit. This project uses an EF Core **SQLite in-memory** database
instead of H2, and simple hand-written fakes instead of Mockito.

```csharp
[Fact]
public async Task CreateAsync_GeneratesSequentialTicketNumbers()
{
    // Arrange / Act / Assert using SqliteTestDatabase + FakeCurrentUserService
}
```

## Mental model

Think of ASP.NET Core as:

- **Spring MVC** for the web layer,
- **Spring Data JPA** for EF Core,
- **Spring Security** for JWT auth,
- **Spring's `@Bean` container** for the built-in DI container,
- but with **interfaces + explicit registration** and **no runtime magic**.
