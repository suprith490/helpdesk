using HelpDesk.Application.DTOs.Users;
using HelpDesk.Domain.Entities;

namespace HelpDesk.Infrastructure.Services.Mapping;

public static class UserMapper
{
    public static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        DepartmentId = user.DepartmentId,
        DepartmentName = user.Department?.Name,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
