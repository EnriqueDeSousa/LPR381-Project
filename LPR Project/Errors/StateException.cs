namespace LPR381Solver.Errors
{
    public class StateException : SolverException
    {
        public StateException(string message)
            : base(ErrorCode.StateError, message)
        {
        }
    }
}
