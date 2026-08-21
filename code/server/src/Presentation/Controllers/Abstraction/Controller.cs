using Ardalis.Result;
using IMT_Reservas.Server.Application.Abstraction;
using Microsoft.AspNetCore.Mvc;
using AppValidationError = IMT_Reservas.Server.Application.Abstraction.ValidationError;

namespace IMT_Reservas.Server.Presentation.Controllers.Abstraction;

[ApiController]
public abstract class Controller : ControllerBase
{
    protected IActionResult ToResponse<T>(Result<T> result)
        where T : class =>
        result.Status switch
        {
            ResultStatus.Ok or ResultStatus.Created => Ok(
                new Response<T> { Status = 200, Value = result.Value }
            ),
            ResultStatus.NotFound => NotFound(
                new Response<object> { Status = 404, Errors = result.Errors.ToList() }
            ),
            ResultStatus.Unauthorized => Unauthorized(
                new Response<object> { Status = 401, Errors = result.Errors.ToList() }
            ),
            ResultStatus.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new Response<object> { Status = 403, Errors = result.Errors.ToList() }
            ),
            ResultStatus.Invalid => BadRequest(
                new Response<object>
                {
                    Status = 400,
                    Errors = result.ValidationErrors.Select(e => e.ErrorMessage).ToList(),
                    ValidationErrors = result
                        .ValidationErrors.Select(e => new AppValidationError
                        {
                            PropertyName = e.Identifier,
                            Description = e.ErrorMessage,
                        })
                        .ToList(),
                }
            ),
            _ => BadRequest(new Response<object> { Status = 400, Errors = result.Errors.ToList() }),
        };

    protected IActionResult ToCreatedResponse<T>(
        Result<T> result,
        string actionName,
        object? routeValues
    )
        where T : class
    {
        if (!result.IsSuccess)
            return ToResponse(result);

        return CreatedAtAction(
            actionName,
            routeValues,
            new Response<T> { Status = 201, Value = result.Value }
        );
    }

    protected IActionResult ToDeleteResponse(Result<object> result)
    {
        if (result.IsSuccess)
            return NoContent();

        if (result.IsNotFound())
            return NotFound(new Response<object> { Status = 404, Errors = result.Errors.ToList() });

        return BadRequest(new Response<object> { Status = 400, Errors = result.Errors.ToList() });
    }
}
