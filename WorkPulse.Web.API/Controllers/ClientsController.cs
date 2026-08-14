using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.DTOs.Clients;
using WorkPulse.Application.Interfaces;
using WorkPulse.Web.API.Contracts.Requests.Clients;
using WorkPulse.Web.API.Contracts.Responses.Clients;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ClientResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetAllAsync(cancellationToken);
        return Ok(clients.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetByIdAsync(id, cancellationToken);
        return Ok(Map(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var created = await _clientService.CreateAsync(new CreateClientRequestDto
        {
            Name = request.Name,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            PhoneNumber = request.PhoneNumber,
            Description = request.Description
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request, CancellationToken cancellationToken)
    {
        await _clientService.UpdateAsync(id, new UpdateClientRequestDto
        {
            Name = request.Name,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            PhoneNumber = request.PhoneNumber,
            Description = request.Description
        }, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _clientService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static ClientResponse Map(ClientDto client) => new()
    {
        Id = client.Id,
        Name = client.Name,
        ContactName = client.ContactName,
        ContactEmail = client.ContactEmail,
        PhoneNumber = client.PhoneNumber,
        Description = client.Description,
        CreatedAt = client.CreatedAt,
        UpdatedAt = client.UpdatedAt,
        ProjectCount = client.ProjectCount,
        OpenTaskCount = client.OpenTaskCount
    };
}