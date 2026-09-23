using HelpDesk.Application.DTOs.Departments;

namespace HelpDesk.Application.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
