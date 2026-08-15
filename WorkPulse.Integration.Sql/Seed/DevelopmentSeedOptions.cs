namespace WorkPulse.Integration.Sql.Seed;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";

    public bool Enabled { get; init; } = true;
    public string AdminEmail { get; init; } = "admin@workpulse.local";
    public string? AdminPassword { get; init; }
    public bool SeedSampleData { get; init; } = true;
}
