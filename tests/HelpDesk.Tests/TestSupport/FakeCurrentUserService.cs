using HelpDesk.Application.Interfaces;

namespace HelpDesk.Tests.TestSupport;

/// <summary>
/// Test double for <see cref="ICurrentUserService"/>. Tests set the properties
/// to simulate the authenticated user that the JWT middleware would normally
/// provide.
/// </summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
}
