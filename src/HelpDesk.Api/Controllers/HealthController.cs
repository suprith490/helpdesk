using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

public sealed class HealthController : ApiControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _dbContext;

    public HealthController(IWebHostEnvironment environment, AppDbContext dbContext)
    {
        _environment = environment;
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "HelpDesk.Api",
            environment = _environment.EnvironmentName,
            timestampUtc = DateTime.UtcNow
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return Ok(new
        {
            database = canConnect ? "reachable" : "unreachable",
            provider = _dbContext.Database.ProviderName
        });
    }
}
