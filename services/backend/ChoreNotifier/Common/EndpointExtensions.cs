using System.Reflection;

namespace ChoreNotifier.Common;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            var method = type.GetMethod(nameof(IEndpoint.Map), BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, [app]);
        }

        return app;
    }
}
