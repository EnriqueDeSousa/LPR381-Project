using System;

namespace LPR381Project.Common.Errors
{
    /// <summary>
    /// Simple Result<T> container to return success/failure without throwing.
    /// Use for library APIs where callers prefer to handle errors explicitly.
    /// </summary>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public SolverException? Error { get; }

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
            Error = null;
        }

        private Result(SolverException error)
        {
            IsSuccess = false;
            Value = default;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(value);
        public static Result<T> Failure(SolverException error) => new Result<T>(error);
    }

    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public SolverException? Error { get; }

        private Result(bool success)
        {
            IsSuccess = success;
            Error = null;
        }

        private Result(SolverException error)
        {
            IsSuccess = false;
            Error = error;
        }

        public static Result Ok() => new Result(true);
        public static Result Failure(SolverException error) => new Result(error);
    }
}
