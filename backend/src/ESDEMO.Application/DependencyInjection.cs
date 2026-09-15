using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(Common.Behaviors.ValidationBehavior<,>));
        });

        return services;
    }
}
