using System;
using System.IO;

namespace LPR381Solver.Errors
{
    /// <summary>Turns a SolverException (or any other exception) into a friendly
    /// message for the console UI and appends a line to error.log for diagnostics.</summary>
    public static class ErrorHandler
    {
        private static readonly string LogFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

        public static void Handle(Exception exception)
        {
            Log(exception);
        }

        public static string GetMessage(Exception exception)
        {
            if (exception is SolverException solverException)
                return GetSolverMessage(solverException);

            return "An unexpected error occurred. Please try again.";
        }

        private static string GetSolverMessage(SolverException exception)
        {
            switch (exception.Code)
            {
                case ErrorCode.InputValidation:
                    return "The input is invalid.\n\n" + exception.Message;
                case ErrorCode.ModelInfeasible:
                    return "The model is infeasible.\n\n" + exception.Message;
                case ErrorCode.ModelUnbounded:
                    return "The model is unbounded.\n\n" + exception.Message;
                case ErrorCode.AlgorithmNotSupported:
                    return "The selected algorithm cannot solve this model.\n\n" + exception.Message;
                case ErrorCode.NumericalError:
                    return "A numerical error occurred while solving the model.\n\n" + exception.Message;
                case ErrorCode.StateError:
                    return "The solver is not ready for this operation.\n\n" + exception.Message;
                case ErrorCode.FileError:
                    return "There was a problem with the input or output file.\n\n" + exception.Message;
                default:
                    return "A solver error occurred.\n\n" + exception.Message;
            }
        }

        private static void Log(Exception exception)
        {
            try
            {
                string message =
                    DateTime.Now + Environment.NewLine +
                    exception + Environment.NewLine +
                    "----------------------------------------" +
                    Environment.NewLine;

                File.AppendAllText(LogFile, message);
            }
            catch
            {
                // Do not allow logging errors to crash the application.
            }
        }
    }
}
