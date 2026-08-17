using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkPulse.Application.Common.Exceptions;

namespace WorkPulse.Web.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was canceled.");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499;
            }
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Not found", ex.Message);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Validation failed", ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627 && ex.Message.Contains("IX_Clients_ContactEmail", StringComparison.OrdinalIgnoreCase))
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict, "Conflict", "A client with this email already exists.");
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "Forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request.");

            if (context.Response.HasStarted)
            {
                return;
            }

            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Server error", "Something went wrong while processing your request.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
