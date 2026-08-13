using System.Data;

namespace WorkPulse.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
