using HelpDesk.Application.Common;
using HelpDesk.Application.DTOs.Tickets;

namespace HelpDesk.Application.Interfaces;

public interface ITicketService
{
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);
    Task<TicketResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TicketDetailResponse> GetDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<TicketResponse>> GetAllAsync(TicketQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<TicketResponse> UpdateAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken = default);
    Task<TicketResponse> AssignAsync(int id, AssignTicketRequest request, CancellationToken cancellationToken = default);
    Task<TicketResponse> ChangeStatusAsync(int id, UpdateTicketStatusRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TicketHistoryResponse>> GetHistoryAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
