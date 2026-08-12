namespace LPR381Project.Common.Errors
{
    public class StateException : SolverException
    {
        public StateException(string message)
            : base(ErrorCode.StateError, message)
        {
        }
    }
}
