using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;

using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>
/// Refuses a request with 400 and <see cref="ApiErrorCodes.InvalidRequest"/> when a value bound from it breaks
/// <see cref="InputRules"/>, before the action runs (MIL-077). It holds for every action, so the use cases and the
/// database never see such a value.
/// </summary>
public sealed class InputGuardFilter : IActionFilter
{
    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (ParameterDescriptor parameter in context.ActionDescriptor.Parameters)
        {
            if (parameter.BindingInfo?.BindingSource is { IsFromRequest: true }
                && context.ActionArguments.TryGetValue(parameter.Name, out object? value)
                && !InputRules.IsAccepted(value))
            {
                context.Result = new BadRequestObjectResult(new ApiResponse([ApiErrorCodes.InvalidRequest]));
                return;
            }
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Nothing to do after the action.
    }
}
