namespace LPR381Solver.Errors
{
    /// <summary>A simple success/failure wrapper so solver pipelines can report a
    /// SolverException without relying on exceptions for ordinary control flow.</summary>
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public SolverException? Error { get; private set; }

        private Result() { }

        public static Result<T> Success(T value) => new Result<T> { IsSuccess = true, Value = value };

        public static Result<T> Failure(SolverException error) => new Result<T> { IsSuccess = false, Error = error };
    }
}
