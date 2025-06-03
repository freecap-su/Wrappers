namespace FreeCap.Client.Exceptions;

/// <summary>
/// Exception thrown when a captcha solving operation times out.
/// </summary>
public class FreeCapTimeoutException : FreeCapException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FreeCapTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FreeCapTimeoutException(string message) : base(message) { }
} 