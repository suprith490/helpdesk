using HelpDesk.Application.Interfaces;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Tests.TestSupport;

/// <summary>
/// Deterministic JWT stub so service tests do not depend on real signing keys.
/// </summary>
public sealed class FakeJwtTokenService : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user) =>
        ($"test-token-for-{user.Id}", DateTime.UtcNow.AddHours(1));
}
