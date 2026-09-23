using HelpDesk.Application.DTOs.Auth;
using HelpDesk.Application.DTOs.Users;

namespace HelpDesk.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
