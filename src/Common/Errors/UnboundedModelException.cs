using System;

namespace LPR381Project.Common.Errors
{
    public class UnboundedModelException : SolverException
    {
        public UnboundedModelException(string message) 
            : base(ErrorCode.ModelUnbounded, message) { }

        public UnboundedModelException(string message, Exception inner) 
            : base(ErrorCode.ModelUnbounded, message, inner) { }
    }
}
