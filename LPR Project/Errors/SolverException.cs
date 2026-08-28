using System;

namespace LPR381Solver.Errors
{
    public class SolverException : Exception
    {
        public ErrorCode Code { get; }

        public SolverException(ErrorCode code, string message)
            : base(message)
        {
            Code = code;
        }

        public SolverException(ErrorCode code, string message, Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
