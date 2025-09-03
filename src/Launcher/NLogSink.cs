using Avalonia.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Launcher
{
    public class NLogSink(LogEventLevel minimumLevel, IList<string>? areas = null) : ILogSink
    {
        private readonly LogEventLevel _level = minimumLevel;
        private readonly HashSet<string>? _areas = areas != null && areas.Count > 0 ? [.. areas] : null;
        private readonly ConcurrentDictionary<string, NLog.ILogger> _loggerCache = new();

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return level >= _level && (_areas?.Contains(area) ?? true);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            if (IsEnabled(level, area))
            {
                var logger = Resolve(source?.GetType(), area);
                logger.Log(LogLevelToNLogLevel(level), messageTemplate);
            }
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (IsEnabled(level, area))
            {
                var logger = Resolve(source?.GetType(), area);
                logger.Log(LogLevelToNLogLevel(level), messageTemplate, propertyValues);
            }
        }
        public NLog.ILogger Resolve(Type? source, string? area)
        {
            var loggerName = source?.FullName ?? area ?? typeof(NLogSink).FullName;

            if (string.IsNullOrEmpty(loggerName))
                loggerName = typeof(NLogSink).FullName;

            // Now, explicitly check again
            if (string.IsNullOrEmpty(loggerName))
                throw new InvalidOperationException("Logger name cannot be null or empty.");

            return _loggerCache.GetOrAdd(loggerName, name => NLog.LogManager.GetLogger(name));
        }

        private static NLog.LogLevel LogLevelToNLogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => NLog.LogLevel.Trace,
                LogEventLevel.Debug => NLog.LogLevel.Debug,
                LogEventLevel.Information => NLog.LogLevel.Info,
                LogEventLevel.Warning => NLog.LogLevel.Warn,
                LogEventLevel.Error => NLog.LogLevel.Error,
                LogEventLevel.Fatal => NLog.LogLevel.Fatal,
                _ => NLog.LogLevel.Off,
            };
        }
    }
}