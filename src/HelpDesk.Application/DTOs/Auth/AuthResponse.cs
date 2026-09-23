using HelpDesk.Application.DTOs.Users;

namespace HelpDesk.Application.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserResponse User { get; set; } = null!;
}
