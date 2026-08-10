using System;

namespace LPR381Project.Common.Errors
{
    /// <summary>
    /// Lightweight, project-scoped error handling/logging facade.
    /// Allows swapping in a different logger (for example, one based on Microsoft.Extensions.Logging).
    /// </summary>
    public static class ErrorHandler
    {
        private static IProjectLogger _logger = new ConsoleProjectLogger();

        /// <summary>
        /// Replace the default logger with an external implementation.
        /// Call this at app startup if you have a DI container or logging framework.
        /// </summary>
        public static void SetLogger(IProjectLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static void Log(SolverException ex, string? context = null)
        {
            _logger.LogError(ex.Code, ex, context ?? ex.Message);
        }

        public static void Log(Exception ex, ErrorCode code = ErrorCode.Unknown, string? context = null)
        {
            if (ex is SolverException s)
            {
                Log(s, context);
                return;
            }

            _logger.LogError(code, ex, context ?? ex.Message);
        }
    }

    public interface IProjectLogger
    {
        void LogError(ErrorCode code, Exception ex, string message);
        void LogInfo(string message);
        void LogDebug(string message);
    }

    internal class ConsoleProjectLogger : IProjectLogger
    {
        public void LogError(ErrorCode code, Exception ex, string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            Console.Error.WriteLine($"[{ts}] ERROR {code}: {message}");
            Console.Error.WriteLine(ex.ToString());
        }

        public void LogInfo(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            Console.Out.WriteLine($"[{ts}] INFO: {message}");
        }

        public void LogDebug(string message)
        {
            var ts = DateTime.UtcNow.ToString("o");
            Console.Out.WriteLine($"[{ts}] DEBUG: {message}");
        }
    }
}
