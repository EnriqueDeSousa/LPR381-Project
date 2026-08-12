namespace LPR381Project.Common.Errors
{
    public class NumericalException : SolverException
    {
        public NumericalException(string message)
            : base(ErrorCode.NumericalError, message)
        {
        }
    }
}
