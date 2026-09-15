using System.ComponentModel.DataAnnotations;
using ESDEMO.Application.Common.Exceptions;
using MediatR;

namespace ESDEMO.Application.Common.Behaviors;

public interface IValidatedRequest
{
    object Payload { get; }
}

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IValidatedRequest validated)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(validated.Payload, new ValidationContext(validated.Payload), results, true))
            {
                var errors = results
                    .SelectMany(result => result.MemberNames.DefaultIfEmpty("request")
                        .Select(member => (Member: member, Error: result.ErrorMessage ?? "Invalid value.")))
                    .GroupBy(result => result.Member)
                    .ToDictionary(group => group.Key, group => group.Select(result => result.Error).ToArray());
                throw new RequestValidationException(errors);
            }
        }

        return next();
    }
}
