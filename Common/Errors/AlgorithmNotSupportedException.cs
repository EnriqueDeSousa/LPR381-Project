namespace LPR381Project.Common.Errors
{
    public class AlgorithmNotSupportedException : SolverException
    {
        public AlgorithmNotSupportedException(string message)
            : base(ErrorCode.AlgorithmNotSupported, message)
        {
        }
    }
}
