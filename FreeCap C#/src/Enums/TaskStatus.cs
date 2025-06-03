namespace FreeCap.Client.Enums;

/// <summary>
/// Represents the status of a captcha solving task.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is waiting to be processed
    /// </summary>
    Pending,
    
    /// <summary>
    /// Task is currently being processed
    /// </summary>
    Processing,
    
    /// <summary>
    /// Task has been successfully solved
    /// </summary>
    Solved,
    
    /// <summary>
    /// Task encountered an error
    /// </summary>
    Error,
    
    /// <summary>
    /// Task failed to be solved
    /// </summary>
    Failed
} 