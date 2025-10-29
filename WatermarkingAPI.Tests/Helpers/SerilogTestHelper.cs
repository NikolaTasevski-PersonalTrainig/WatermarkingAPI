using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Threading;

namespace WatermarkingAPI.Tests.Helpers
{
    public class SerilogTestHelper
    {
        private readonly ConcurrentBag<LogEvent> _logEvents = new ConcurrentBag<LogEvent>();
        private readonly string _filePath;

        public Serilog.ILogger CreateTestLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.Sink(new TestSink(_logEvents, "test-logs.txt"))
                .CreateLogger();
        }

        public IList<LogEvent> GetLogEvents() => _logEvents.ToList();

        public void ClearLogEvents() => _logEvents.Clear();

        // Fixed method - uses LogEventLevel (Serilog) instead of LogLevel (Microsoft)
        public bool HasLogEvent(LogEventLevel level, string messageTemplate) =>
            _logEvents.Any(e => e.Level == level && e.MessageTemplate.Text.Contains(messageTemplate));

        public LogEvent GetLogEvent(LogEventLevel level, string messageTemplate) =>
            _logEvents.FirstOrDefault(e => e.Level == level && e.MessageTemplate.Text.Contains(messageTemplate));

        // Additional helper methods
        public bool HasInformation(string messageTemplate) =>
            HasLogEvent(LogEventLevel.Information, messageTemplate);

        public bool HasError(string messageTemplate) =>
            HasLogEvent(LogEventLevel.Error, messageTemplate);

        public bool HasWarning(string messageTemplate) =>
            HasLogEvent(LogEventLevel.Warning, messageTemplate);
    }

    public class TestSink : ILogEventSink
    {
        private readonly ConcurrentBag<LogEvent> _logEvents;
        private readonly object _lock = new object();
        private readonly string _filePath;

        public TestSink(ConcurrentBag<LogEvent> logEvents, string filePath)
        {
            _logEvents = logEvents;
            _filePath = filePath;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            var fullMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logEvent.Level}] {message}{Environment.NewLine}";

            lock (_lock)
            {
                // Use FileShare.ReadWrite to allow other processes to read the file
                using (var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(fullMessage);
                }
            }
        }
    }
}
