using Microsoft.Extensions.Logging;

namespace WorkPulse.Integration.Sql.Seed;

public interface IDatabaseSeeder
{
    Task SeedAsync(ILogger logger, CancellationToken cancellationToken = default);
}
