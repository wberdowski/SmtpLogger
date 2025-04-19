using Microsoft.Extensions.Logging;

namespace SmtpLogger
{
    internal readonly struct LogMessageEntry
    {
        public readonly LogLevel LogLevel;
        public readonly string CategoryName;

        public readonly string Message;

        public LogMessageEntry(LogLevel logLevel, string categoryName, string message)
        {
            LogLevel = logLevel;
            CategoryName = categoryName;
            Message = message;
        }
    }
}
