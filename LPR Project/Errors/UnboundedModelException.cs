namespace LPR381Solver.Errors
{
    public class UnboundedModelException : SolverException
    {
        public UnboundedModelException(string message)
            : base(ErrorCode.ModelUnbounded, message)
        {
        }
    }
}
