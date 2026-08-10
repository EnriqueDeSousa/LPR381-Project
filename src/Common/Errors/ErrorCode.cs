namespace LPR381Project.Common.Errors
{
    /// <summary>
    /// High-level error categories the solver can produce.
    /// Useful for telemetry and handling policy decisions.
    /// </summary>
    public enum ErrorCode
    {
        Unknown = 0,
        InputValidation = 100,
        ModelInfeasible = 200,
        ModelUnbounded = 201,
        NumericalError = 300,
        StateError = 400,
        ConfigurationError = 500,
        Timeout = 600,
        ResourceExhausted = 700
    }
}
