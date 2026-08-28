namespace LPR381Solver.Errors
{
    public class InfeasibleModelException : SolverException
    {
        public InfeasibleModelException(string message)
            : base(ErrorCode.ModelInfeasible, message)
        {
        }
    }
}
