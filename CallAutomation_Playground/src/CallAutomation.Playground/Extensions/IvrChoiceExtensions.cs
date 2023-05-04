using CallAutomation.Playground.Services;

namespace CallAutomation.Playground.Extensions;

public static class IvrChoiceExtensions
{
    public static async Task InvokeChoice(this IvrChoice? ivrChoice, Func<Task> successFunction, Func<Task> errorFunction)
    {
        if (ivrChoice is null)
        {
            await errorFunction();
        }
        else
        {
            await successFunction();
        }
    }
}