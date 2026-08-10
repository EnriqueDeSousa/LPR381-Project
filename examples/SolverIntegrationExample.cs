using System;
using LPR381Project.Common.Errors;

namespace LPR381Project.ExampleUsage
{
    public class SolverRunner
    {
        // Example: a public facade method which uses exceptions internally but returns a Result.
        public Result<Solution> RunSolver(Model model, TimeSpan timeout)
        {
            try
            {
                ValidateModel(model);

                // Call internal solver implementation (example)
                var solution = InternalSolve(model, timeout);

                if (solution == null)
                    return Result<Solution>.Failure(new InfeasibleModelException("Solver returned no solution."));

                return Result<Solution>.Success(solution);
            }
            catch (SolverException se)
            {
                // Known solver exception: log and return the failure
                ErrorHandler.Log(se, "RunSolver failed");
                return Result<Solution>.Failure(se);
            }
            catch (Exception e)
            {
                // Unknown exceptions wrapped into a SolverException for consistency
                var wrapped = new SolverException(ErrorCode.Unknown, "Unhandled exception in RunSolver", e);
                ErrorHandler.Log(wrapped, "RunSolver failed with unexpected exception");
                return Result<Solution>.Failure(wrapped);
            }
        }

        private void ValidateModel(Model model)
        {
            if (model == null)
                throw new InputValidationException("Model cannot be null.");

            if (!model.HasVariables)
                throw new InputValidationException("Model must contain variables.");

            // more validation...
        }

        private Solution? InternalSolve(Model model, TimeSpan timeout)
        {
            // Implementation-specific call; shown as a placeholder
            throw new NotImplementedException();
        }
    }

    // Placeholder types — replace with your real types
    public class Model { public bool HasVariables { get; set; } }
    public class Solution { }
}
