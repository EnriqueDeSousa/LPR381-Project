namespace LPR381Solver.Errors
{
    public class AlgorithmNotSupportedException : SolverException
    {
        public AlgorithmNotSupportedException(string message)
            : base(ErrorCode.AlgorithmNotSupported, message)
        {
        }
    }
}
