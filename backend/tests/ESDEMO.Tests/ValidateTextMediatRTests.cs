using ESDEMO.Application;
using ESDEMO.Application.Examples.Queries.ValidateText;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ESDEMO.Tests;

public sealed class ValidateTextMediatRTests
{
    [Fact]
    public async Task Sender_dispatches_query_to_registered_handler()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        await using var serviceProvider = services.BuildServiceProvider();
        var sender = serviceProvider.GetRequiredService<ISender>();
        var query = new ValidateTextQuery("ESDEMO");

        var result = await sender.Send(query, CancellationToken.None);

        Assert.Equal("ESDEMO", result.Text);
        Assert.Equal(6, result.Length);
    }
}
