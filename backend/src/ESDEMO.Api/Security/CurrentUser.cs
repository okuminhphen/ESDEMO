using System.Security.Claims;
using ESDEMO.Application.Auth.Abstractions;

namespace ESDEMO.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;
}
