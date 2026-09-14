namespace ESDEMO.Application.Auth.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
