namespace LPR381Project.Common.Errors
{
    public class UnboundedModelException : SolverException
    {
        public UnboundedModelException(string message)
            : base(ErrorCode.ModelUnbounded, message)
        {
        }
    }
}

throw new AlgorithmNotSupportedException(
    "Primal Simplex cannot solve a binary integer programming model."
);