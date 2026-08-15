using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WorkPulse.Integration.Identity.Authentication;
using WorkPulse.Integration.Identity.DependencyInjection;
using WorkPulse.Integration.Sql.DependencyInjection;
using WorkPulse.Integration.Sql.Migrations;
using WorkPulse.Integration.Sql.Seed;
using WorkPulse.Web.API.DependencyInjection;
using WorkPulse.Web.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkPulseWebApi();
builder.Services.AddWorkPulseIdentityIntegration(builder.Configuration);
builder.Services.AddWorkPulseSqlIntegration(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("JWT settings are missing.");

if (string.IsNullOrWhiteSpace(jwt.SecretKey))
{
    throw new InvalidOperationException("JWT secret is missing. Set Jwt__SecretKey as an environment variable or user secret.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WorkPulse API",
        Version = "v1"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("WorkPulse.Startup");

    await bootstrapper.EnsureDatabaseExistsAsync();
    await migrationRunner.MigrateUpAsync();
    await seeder.SeedAsync(startupLogger);
}

app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
