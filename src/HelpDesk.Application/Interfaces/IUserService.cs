using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Users;

namespace HelpDesk.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<UserResponse>> GetAllAsync(UserQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
