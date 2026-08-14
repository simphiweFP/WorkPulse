using WorkPulse.Application.DTOs.Clients;

namespace WorkPulse.Application.Interfaces;

public interface IClientService
{
    Task<IReadOnlyCollection<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(CreateClientRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateClientRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}