namespace LPR381Solver.Errors
{
    public class InputValidationException : SolverException
    {
        public InputValidationException(string message)
            : base(ErrorCode.InputValidation, message)
        {
        }
    }
}
