using MediatR;

namespace ESDEMO.Application.Examples.Queries.ValidateText;

internal sealed class ValidateTextQueryHandler
    : IRequestHandler<ValidateTextQuery, ValidateTextResult>
{
    public Task<ValidateTextResult> Handle(
        ValidateTextQuery request,
        CancellationToken cancellationToken)
    {
        var result = new ValidateTextResult(request.Text, request.Text.Length);

        return Task.FromResult(result);
    }
}
