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

if (pivot == 0)
{
    throw new NumericalException(
        "Cannot perform pivot operation because the pivot is zero."
    );
}