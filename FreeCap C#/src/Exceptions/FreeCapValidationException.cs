namespace FreeCap.Client.Exceptions;

/// <summary>
/// Exception thrown when task validation fails.
/// </summary>
public class FreeCapValidationException : FreeCapException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FreeCapValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FreeCapValidationException(string message) : base(message) { }
} 