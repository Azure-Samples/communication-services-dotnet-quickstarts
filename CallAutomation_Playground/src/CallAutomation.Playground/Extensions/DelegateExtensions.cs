using System.Reflection;

namespace CallAutomation.Playground.Extensions;

public static class DelegateExtensions
{
    public static bool IsAwaitable(this Delegate del)
    {
        Type returnType = del.Method.ReturnType;
        TypeInfo typeInfo = returnType.GetTypeInfo();

        // Check if the return type is Task or a subclass of Task
        if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return true;
        }

        if (typeof(Task).IsAssignableFrom(returnType))
        {
            return true;
        }

        return false;
    }
}