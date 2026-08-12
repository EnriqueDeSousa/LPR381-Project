namespace LPR381Project.Common.Errors
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }

        public T Value { get; private set; }

        public SolverException Error { get; private set; }

        private Result()
        {
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Value = value
            };
        }

        public static Result<T> Failure(SolverException error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Error = error
            };
        }
    }
}
