namespace FreeCap.Client.Exceptions;

/// <summary>
/// Exception thrown when the FreeCap API returns an error response.
/// </summary>
public class FreeCapApiException : FreeCapException
{
    /// <summary>
    /// Gets the HTTP status code associated with the error, if available.
    /// </summary>
    public int? StatusCode { get; }
    
    /// <summary>
    /// Gets the response data from the API, if available.
    /// </summary>
    public Dictionary<string, object>? ResponseData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FreeCapApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code, if available.</param>
    /// <param name="responseData">The response data from the API, if available.</param>
    public FreeCapApiException(string message, int? statusCode = null, Dictionary<string, object>? responseData = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ResponseData = responseData;
    }
} 