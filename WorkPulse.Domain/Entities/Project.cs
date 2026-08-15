using WorkPulse.Domain.Enums;

namespace WorkPulse.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public int OpenTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }

    public bool CanAcceptNewTasks() => Status == ProjectStatus.Active;
}
