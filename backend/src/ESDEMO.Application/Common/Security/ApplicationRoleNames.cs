namespace ESDEMO.Application.Common.Security;

public static class ApplicationRoleNames
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static readonly IReadOnlyCollection<string> All = [Admin, Customer];
}
