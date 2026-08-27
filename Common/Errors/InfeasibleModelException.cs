namespace LPR381Project.Common.Errors
{
    public class InfeasibleModelException : SolverException
    {
        public InfeasibleModelException(string message)
            : base(ErrorCode.ModelInfeasible, message)
        {
        }
    }
}
