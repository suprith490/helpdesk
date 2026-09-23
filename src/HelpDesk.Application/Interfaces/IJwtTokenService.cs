using HelpDesk.Domain.Entities;

namespace HelpDesk.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
