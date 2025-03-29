using System;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

public static class LoggerFactoryInstance
{
    private static ILoggerFactory? _loggerFactory;

    public static void Configure(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public static ILogger<T> CreateLogger<T>()
    {
        if (_loggerFactory == null)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddNLog();
            });
        }
        return _loggerFactory.CreateLogger<T>();
    }
}