namespace LPR381Solver.Errors
{
    public class NumericalException : SolverException
    {
        public NumericalException(string message)
            : base(ErrorCode.NumericalError, message)
        {
        }
    }
}
