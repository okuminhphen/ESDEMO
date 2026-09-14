using MediatR;

namespace ESDEMO.Application.Examples.Queries.ValidateText;

public sealed record ValidateTextQuery(string Text) : IRequest<ValidateTextResult>;

public sealed record ValidateTextResult(string Text, int Length);
