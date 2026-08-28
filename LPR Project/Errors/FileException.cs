namespace LPR381Solver.Errors
{
    public class FileException : SolverException
    {
        public FileException(string message)
            : base(ErrorCode.FileError, message)
        {
        }
    }
}
