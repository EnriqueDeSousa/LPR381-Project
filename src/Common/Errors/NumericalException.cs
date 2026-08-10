using System;

namespace LPR381Project.Common.Errors
{
    public class NumericalException : SolverException
    {
        public NumericalException(string message) 
            : base(ErrorCode.NumericalError, message) { }

        public NumericalException(string message, Exception inner) 
            : base(ErrorCode.NumericalError, message, inner) { }
    }
}
