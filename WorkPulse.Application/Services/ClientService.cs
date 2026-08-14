using System.ComponentModel.DataAnnotations;
using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Clients;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Application.Services;

public sealed class ClientService : IClientService
{
    private static readonly EmailAddressAttribute EmailValidator = new();

    private readonly IClientRepository _clientRepository;
    private readonly IClock _clock;

    public ClientService(IClientRepository clientRepository, IClock clock)
    {
        _clientRepository = clientRepository;
        _clock = clock;
    }

    public Task<IReadOnlyCollection<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => GetAllAsyncImpl(cancellationToken);

    public async Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        return client is null ? throw new NotFoundException($"Client '{id}' was not found.") : Map(client);
    }

    public async Task<ClientDto> CreateAsync(CreateClientRequestDto request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.ContactEmail);

        var now = _clock.UtcNow;
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        await _clientRepository.CreateAsync(client, cancellationToken);
        return Map(client);
    }

    public async Task UpdateAsync(Guid id, UpdateClientRequestDto request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.ContactEmail);

        var existing = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Client '{id}' was not found.");
        }

        var now = _clock.UtcNow;
        await _clientRepository.UpdateAsync(new Client
        {
            Id = id,
            Name = request.Name.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Description = request.Description.Trim(),
            CreatedAt = existing.CreatedAt,
            UpdatedAt = now,
            IsDeleted = false
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _clientRepository.ExistsByIdAsync(id, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Client '{id}' was not found.");
        }

        await _clientRepository.DeleteAsync(id, cancellationToken);
    }

    private static ClientDto Map(Client client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        ContactName = client.ContactName,
        ContactEmail = client.ContactEmail,
        PhoneNumber = client.PhoneNumber,
        Description = client.Description,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
        ProjectCount = 0,
        OpenTaskCount = 0
    };

    private static void Validate(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new WorkPulse.Application.Common.Exceptions.ValidationException("Client name is required.");
        }

        if (!EmailValidator.IsValid(email))
        {
            throw new WorkPulse.Application.Common.Exceptions.ValidationException("A valid contact email is required.");
        }
    }

    private async Task<IReadOnlyCollection<ClientDto>> GetAllAsyncImpl(CancellationToken cancellationToken)
        => (await _clientRepository.GetAllAsync(cancellationToken)).Select(Map).ToArray();
}