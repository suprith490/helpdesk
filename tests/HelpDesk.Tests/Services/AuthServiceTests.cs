using HelpDesk.Application.DTOs.Auth;
using HelpDesk.Application.Exceptions;
using HelpDesk.Domain.Enums;
using HelpDesk.Infrastructure.Security;
using HelpDesk.Infrastructure.Services;
using HelpDesk.Tests.TestSupport;

namespace HelpDesk.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly PasswordHasher _passwordHasher = new();
    private readonly FakeCurrentUserService _currentUser = new();

    private AuthService CreateSut() => new(
        _database.Context,
        _passwordHasher,
        new FakeJwtTokenService(),
        _currentUser);

    [Fact]
    public async Task RegisterAsync_CreatesEmployee_AndReturnsToken()
    {
        var sut = CreateSut();

        var response = await sut.RegisterAsync(new RegisterRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "Jane@Example.com",
            Password = "Passw0rd!"
        });

        Assert.Equal("jane@example.com", response.User.Email);
        Assert.Equal(UserRole.Employee, response.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.NotNull(await _database.Context.Users.FindAsync(response.User.Id));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsConflict_WhenEmailAlreadyExists()
    {
        var sut = CreateSut();
        var request = new RegisterRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            Password = "Passw0rd!"
        };

        await sut.RegisterAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => sut.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_ForValidCredentials()
    {
        var sut = CreateSut();
        TestData.AddUser(
            _database.Context,
            "jane@example.com",
            passwordHash: _passwordHasher.Hash("Passw0rd!"));

        var response = await sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = "Passw0rd!"
        });

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUnauthorized_ForWrongPassword()
    {
        var sut = CreateSut();
        TestData.AddUser(
            _database.Context,
            "jane@example.com",
            passwordHash: _passwordHasher.Hash("Passw0rd!"));

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = "not-the-password"
        }));
    }

    [Fact]
    public async Task LoginAsync_ThrowsForbidden_WhenAccountIsDeactivated()
    {
        var sut = CreateSut();
        TestData.AddUser(
            _database.Context,
            "jane@example.com",
            isActive: false,
            passwordHash: _passwordHasher.Hash("Passw0rd!"));

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "jane@example.com",
            Password = "Passw0rd!"
        }));
    }

    public void Dispose() => _database.Dispose();
}
