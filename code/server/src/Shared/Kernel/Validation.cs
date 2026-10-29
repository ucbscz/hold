using Ardalis.Result;
using FluentValidation.Results;
using ResultValidationError = Ardalis.Result.ValidationError;

namespace IMT_Reservas.Server.Application.Abstraction;

public static class Validation
{
    public static Result<T> ToResult<T>(this ValidationResult validation)
        where T : class =>
        validation.IsValid
            ? Result<T>.Success(null!)
            : Result<T>.Invalid(
                validation.Errors.Select(error => new ResultValidationError
                {
                    Identifier = error.PropertyName,
                    ErrorMessage = error.ErrorMessage,
                })
            );
}
