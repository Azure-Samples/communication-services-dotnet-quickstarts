namespace CallAutomation.Playground.Exceptions;

public class InvalidEntryException : Exception
{
    public InvalidEntryException(string message)
        : base(message)
    {
        
    }
}