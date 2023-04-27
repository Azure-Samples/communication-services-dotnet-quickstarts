using CallAutomation.Playground.Menus;

namespace CallAutomation.Playground.Services;

public class IvrMenuRegistry
{
    public Dictionary<string, IvrMenu> IvrMenus { get; } = new();
}