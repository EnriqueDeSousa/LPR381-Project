namespace LPR381Project.Common.Errors
{
    public class FileException : SolverException
    {
        public FileException(string message)
            : base(ErrorCode.FileError, message)
        {
        }
    }
}
