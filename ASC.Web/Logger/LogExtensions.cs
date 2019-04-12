using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASC.Business.Interfaces;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;


namespace ASC.Web.Logger
{
    public class AzureStorageLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly ILogDataOperations _logOperations;

        public AzureStorageLogger(string categoryName, Func<string, LogLevel, bool> filter, ILogDataOperations logOperations)
        {
            _categoryName = categoryName;
            _filter = filter;
            _logOperations = logOperations;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (exception==null)
            {
                _logOperations.CreateLogAsync(logLevel.ToString(), formatter(state, exception));
            }
            else
            {
                _logOperations.CreateExceptionLogAsync(eventId.Name, exception.Message, exception.StackTrace);

            }

        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (_filter == null || _filter(_categoryName, logLevel));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }

    //KREIRA INSTACU OD ILOGGER-A
    public class AzureStorageLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly ILogDataOperations _logOperations;
        public AzureStorageLoggerProvider(Func<string, LogLevel, bool> filter, ILogDataOperations logOperations)
        {
            _logOperations = logOperations;
            _filter = filter;
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new AzureStorageLogger(categoryName, _filter, _logOperations);
        }
    }

    public static class EmailLoggerExtensions
    {
        public static ILoggerFactory AddAzureTableStorageLog(this ILoggerFactory factory,
            ILogDataOperations logOperations, Func<string, LogLevel, bool> fileter = null)
        {
            factory.AddProvider(new AzureStorageLoggerProvider(fileter,logOperations));
            return factory;
        }
        
    }
}
