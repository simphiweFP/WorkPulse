namespace WorkPulse.Web.API.Contracts.Responses.Clients;

public sealed class ClientResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int ProjectCount { get; init; }
    public int OpenTaskCount { get; init; }
}