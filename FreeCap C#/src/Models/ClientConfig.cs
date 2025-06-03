namespace FreeCap.Client.Models;

/// <summary>
/// Configuration options for the FreeCap client.
/// </summary>
public class ClientConfig
{
    /// <summary>
    /// The base URL for the FreeCap API. Defaults to "https://freecap.su".
    /// </summary>
    public string ApiUrl { get; set; } = "https://freecap.su";
    
    /// <summary>
    /// Timeout for individual HTTP requests. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Maximum number of retries for failed requests. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Delay between retry attempts. Defaults to 1 second.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Default timeout for task completion. Defaults to 120 seconds.
    /// </summary>
    public TimeSpan DefaultTaskTimeout { get; set; } = TimeSpan.FromSeconds(120);
    
    /// <summary>
    /// Default interval for checking task status. Defaults to 3 seconds.
    /// </summary>
    public TimeSpan DefaultCheckInterval { get; set; } = TimeSpan.FromSeconds(3);
    
    /// <summary>
    /// User agent string to use for HTTP requests.
    /// </summary>
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";
} 