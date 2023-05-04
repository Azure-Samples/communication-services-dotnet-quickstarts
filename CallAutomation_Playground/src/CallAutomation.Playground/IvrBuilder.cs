using CallAutomation.Playground.Interfaces;
using CallAutomation.Playground.Services;

namespace CallAutomation.Playground;

public class IvrBuilder
{
    private readonly IServiceCollection _services;
    private readonly string _name;
    private readonly Dictionary<Type, Type> _choices = new();

    private IvrConfiguration _ivrConfiguration;

    public IvrBuilder(IServiceCollection services, string name)
    {
        _services = services;
        _name = name;
    }

    public IvrBuilder WithConfiguration(Action<IvrConfiguration> options)
    {
        var ivrConfiguration = new IvrConfiguration();
        options(ivrConfiguration);
        _ivrConfiguration = ivrConfiguration;
        return this;
    }

    public IvrBuilder AddChoice<TTone, THandler>()
        where TTone : IDtmfTone
        where THandler : IvrChoice
    {
        _services.AddScoped<THandler>();
        _choices.Add(typeof(TTone), typeof(THandler));
        return this;
    }

    public void Build()
    {
        if (!_choices.Any())
        {
            throw new ApplicationException("No IVR choices added to the IVR Menu.");
        }

        _services.AddSingleton(serviceProvider =>
        {
            var registry = new IvrMenuRegistry();
            registry.IvrMenus.Add(_name, new IvrMenu(serviceProvider, _choices, _ivrConfiguration));
            return registry;
        });
    }
}