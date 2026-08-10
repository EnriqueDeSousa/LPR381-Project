using System;

namespace LPR381Project.Common.Errors
{
    /// <summary>
    /// Base exception type for solver-related errors.
    /// </summary>
    public class SolverException : Exception
    {
        public ErrorCode Code { get; }

        public SolverException(ErrorCode code, string message) : base(message)
        {
            Code = code;
        }

        public SolverException(ErrorCode code, string message, Exception inner) : base(message, inner)
        {
            Code = code;
        }
    }
}
