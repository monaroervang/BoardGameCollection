using Domain.Exceptions.CustomExceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace API.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Convert known domain validation errors into HTTP 400 responses.
        if (exception is not (
            InvalidPlayerCountException
            or RequiredFieldException
            or MaxLengthExceededException
            or UserMinimumAgeException //My API integration tests caught this exception when trying to create a user with an age less than 12 years old.
            or LoanDateInPastException
            or DueDateException
            or ReturnDateException))
            {
            return false;
        }

        httpContext.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request data",
            Detail = exception.Message
        };

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }
}