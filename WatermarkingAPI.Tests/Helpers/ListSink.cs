using Serilog.Core;
using Serilog.Events;

public class ListSink : ILogEventSink
{
    private readonly List<string> _logMessages;

    public ListSink(List<string> logMessages)
    {
        _logMessages = new List<string>();
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        _logMessages.Add(message);
    }
}