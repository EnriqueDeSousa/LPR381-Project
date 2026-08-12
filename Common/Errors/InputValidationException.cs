namespace LPR381Project.Common.Errors
{
    public class InputValidationException : SolverException
    {
        public InputValidationException(string message)
            : base(ErrorCode.InputValidation, message)
        {
        }
    }
}
