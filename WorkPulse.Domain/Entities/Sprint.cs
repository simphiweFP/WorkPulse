using WorkPulse.Domain.Enums;

namespace WorkPulse.Domain.Entities;

public class Sprint
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planned;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int TotalTasks { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int TotalPoints { get; set; }
    public int CompletedPoints { get; set; }
    public double ProgressPercent => TotalPoints <= 0 ? 0 : Math.Round((double)CompletedPoints / TotalPoints * 100, 0);
}
