using System;

namespace LPR381Project.Common.Errors
{
    public class InfeasibleModelException : SolverException
    {
        public InfeasibleModelException(string message) 
            : base(ErrorCode.ModelInfeasible, message) { }

        public InfeasibleModelException(string message, Exception inner) 
            : base(ErrorCode.ModelInfeasible, message, inner) { }
    }
}
