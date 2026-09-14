namespace ESDEMO.Application.Abstractions.Cqrs;

public interface IQuery<out TResult>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}
