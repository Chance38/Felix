using Felix.Common;
using FluentValidation;

namespace Felix.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.FirstOrDefault(x => x is T) as T;
        if (request is null)
        {
            var error = new ApiErrorResponse
            {
                Errors = [new ApiError { Message = "Request body is required" }]
            };
            return Results.BadRequest(error);
        }

        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ApiError
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
                .ToList();

            return Results.BadRequest(new ApiErrorResponse { Errors = errors });
        }

        return await next(context);
    }
}
