using System.ComponentModel.DataAnnotations;
using HelpDesk.Domain.Enums;

namespace HelpDesk.Application.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}
