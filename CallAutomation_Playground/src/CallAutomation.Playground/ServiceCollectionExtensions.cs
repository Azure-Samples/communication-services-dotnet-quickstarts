namespace CallAutomation.Playground;

public static class ServiceCollectionExtensions
{
    public static IvrBuilder AddIvrMenu(this IServiceCollection services, string name, Action<IvrBuilder> options)
    {
        var builder = new IvrBuilder(services, name);
        options(builder);
        return builder;
    }
}