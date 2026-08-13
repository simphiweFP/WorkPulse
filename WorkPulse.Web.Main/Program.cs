using Microsoft.OpenApi;
using WorkPulse.Application;
using WorkPulse.Infrastructure;
using WorkPulse.Integration.Sql.DependencyInjection;
using WorkPulse.Web.API.DependencyInjection;
using WorkPulse.Web.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWorkPulseWebApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkPulseSqlIntegration(builder.Configuration);

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

await DatabaseInitializer.InitializeAsync(app.Services);

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
