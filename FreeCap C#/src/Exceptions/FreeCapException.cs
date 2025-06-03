namespace FreeCap.Client.Exceptions;

/// <summary>
/// Base exception class for all FreeCap-related errors.
/// </summary>
public class FreeCapException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FreeCapException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FreeCapException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FreeCapException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FreeCapException(string message, Exception innerException) : base(message, innerException) { }
} 