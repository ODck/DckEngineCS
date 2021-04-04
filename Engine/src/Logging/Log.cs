using System;
using Dck.Engine.Settings;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

namespace Dck.Engine.Logging
{
    /// <summary>
    ///     Static Logger to Log messages into all configured sinks
    ///     Configure Serilog
    ///     https://github.com/serilog/serilog-settings-configuration
    /// </summary>
    public static class Log
    {
        private static ILogger _logger;

        private static ILogger CoreLogger
        {
            get
            {
                if (_logger != null)
                    return _logger;
                Init();
                return _logger!;
            }
        }

        private static void Init()
        {
            //Logger already initialized
            if (_logger != null)
                return;
            var outputTemplate = "[{Timestamp:HH:mm:ss}]";
            outputTemplate += $" {Constants.Title}: ";
            outputTemplate += "[{Level}]: {Message:lj}{NewLine}{Exception}";

            _logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .WriteTo.Console(
                    outputTemplate: outputTemplate,
                    theme: SystemConsoleTheme.Literate)
                .MinimumLevel.Verbose()
                .CreateLogger();
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += ExceptionHandler;
        }

        public static void Dispose()
        {
            if (CoreLogger is IDisposable disposable) disposable.Dispose();
        }

        /// <summary>
        ///     Handles and Log the UnHandledExceptions thrown by the code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception) args.ExceptionObject;
            Fatal(e);
        }

        #region LoggerMethods

        /// <summary>
        ///     Format the string to show first the caller type
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static string FormatForContext<T>(this string message)
        {
            return $"{typeof(T).Name} - {message}";
        }

        /// <summary>
        ///     Format the string to show first the caller type
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string FormatForContext(this object message)
        {
            return $"{message.GetType().Name} - {message}";
        }

        /// <summary>
        ///     Log a message if certain condition is not true
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="failedAssertionMessage"></param>
        public static void Assert(bool assertion, string failedAssertionMessage)
        {
            if (!assertion) Error(failedAssertionMessage);
        }

        /// <summary>
        ///     Log a debug message to all sinks
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            CoreLogger.Debug("{Message}", message);
        }

        /// <summary>
        ///     Log a debug message to all sinks
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public static void Debug<T>(string message)
        {
            CoreLogger.Debug("{Message}", message.FormatForContext<T>());
        }

        /// <summary>
        ///     Log a debug object.ToString() to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Debug(object obj)
        {
            CoreLogger.Debug("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a info message to all sinks
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            CoreLogger.Information("{Message}", message);
        }

        /// <summary>
        ///     Log a info message to all sinks
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public static void Info<T>(string message)
        {
            CoreLogger.Information("{Message}", message.FormatForContext<T>());
        }

        /// <summary>
        ///     Log a info object.ToString() to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Info(object obj)
        {
            CoreLogger.Information("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a warning message to all sinks
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message)
        {
            CoreLogger.Warning("{Message}", message);
        }

        /// <summary>
        ///     Log a warning message to all sinks
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public static void Warning<T>(string message)
        {
            CoreLogger.Warning("{Message}", message.FormatForContext<T>());
        }

        /// <summary>
        ///     Log a warning object.ToString() to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Warning(object obj)
        {
            CoreLogger.Warning("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a error message to all sinks
        /// </summary>
        /// <param name="message"></param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void Error(string message)
        {
            CoreLogger.Error("{Message}", message);
        }

        /// <summary>
        ///     Log a error message to all sinks
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public static void Error<T>(string message)
        {
            CoreLogger.Error("{Message}", message.FormatForContext<T>());
        }

        /// <summary>
        ///     Log a error message to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Error(object obj)
        {
            CoreLogger.Error("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a error message to all sinks
        /// </summary>
        /// <param name="ex"></param>
        public static void Error(Exception ex)
        {
            CoreLogger.Error(ex, "{Exception}", ex.Message);
        }


        /// <summary>
        ///     Log a StackTrace / Fatal message to all sinks
        /// </summary>
        /// <param name="message"></param>
        public static void Fatal(string message)
        {
            CoreLogger.Fatal(new Exception(message), "{Message}", message);
        }


        /// <summary>
        ///     Log a StackTrace / Fatal message to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Fatal(object obj)
        {
            CoreLogger.Fatal("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a StackTrace / Fatal message to all sinks
        /// </summary>
        /// <param name="ex"></param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void Fatal(Exception ex)
        {
            CoreLogger.Fatal(ex, "{Exception}", ex.Message);
        }


        /// <summary>
        ///     Log a verbose message to all sinks
        /// </summary>
        /// <param name="message"></param>
        public static void Trace(string message)
        {
            CoreLogger.Verbose(new Exception(message), "{Message}", message);
        }

        /// <summary>
        ///     Log a verbose message to all sinks
        /// </summary>
        /// <param name="obj"></param>
        public static void Trace(object obj)
        {
            CoreLogger.Verbose("{Obj}", obj.ToString());
        }

        /// <summary>
        ///     Log a verbose message to all sinks
        /// </summary>
        /// <param name="ex"></param>
        public static void Trace(Exception ex)
        {
            CoreLogger.Verbose(ex, "{Exception}", ex.Message);
        }

        #endregion
    }
}