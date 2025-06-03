using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FreeCap.Client.Enums;
using FreeCap.Client.Extensions;
using FreeCap.Client.Models;
using FreeCap.Client.Exceptions;

namespace FreeCap.Client;

/// <summary>
/// Professional async client for FreeCap captcha solving service.
/// 
/// Features:
/// - Full support for all captcha types
/// - Robust error handling and retries
/// - IDisposable/IAsyncDisposable support
/// - Comprehensive logging
/// - Type safety with enums and classes
/// - Production-ready configuration options
/// </summary>
public class FreeCapClient : IDisposable, IAsyncDisposable
{
    private readonly string _apiKey;
    private readonly ClientConfig _config;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    /// <summary>
    /// Initialize the FreeCap client.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key</param>
    /// <param name="config">Client configuration options</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="httpClient">Optional HttpClient instance</param>
    public FreeCapClient(
        string apiKey,
        ClientConfig? config = null,
        ILogger? logger = null,
        HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new FreeCapValidationException("API key cannot be empty");

        _apiKey = apiKey.Trim();
        _config = config ?? new ClientConfig();
        _logger = logger ?? NullLogger.Instance;

        if (!_config.ApiUrl.StartsWith("http://") && !_config.ApiUrl.StartsWith("https://"))
            throw new FreeCapValidationException("API URL must start with http:// or https://");

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
        
        _httpClient.Timeout = _config.RequestTimeout;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    /// <summary>
    /// Validates a captcha task configuration for the specified captcha type.
    /// </summary>
    /// <param name="task">The captcha task to validate.</param>
    /// <param name="captchaType">The type of captcha being solved.</param>
    /// <exception cref="FreeCapValidationException">Thrown when validation fails.</exception>
    private void ValidateTask(CaptchaTask task, CaptchaType captchaType)
    {
        switch (captchaType)
        {
            case CaptchaType.HCaptcha:
                if (string.IsNullOrWhiteSpace(task.SiteKey))
                    throw new FreeCapValidationException("sitekey is required for hCaptcha");
                if (string.IsNullOrWhiteSpace(task.SiteUrl))
                    throw new FreeCapValidationException("siteurl is required for hCaptcha");
                if (string.IsNullOrWhiteSpace(task.GroqApiKey))
                    throw new FreeCapValidationException("groq_api_key is required for hCaptcha");
                if (string.IsNullOrWhiteSpace(task.RqData))
                    throw new FreeCapValidationException("rqdata cannot be blank for Discord hCaptcha");
                break;

            case CaptchaType.CaptchaFox:
                if (string.IsNullOrWhiteSpace(task.SiteKey))
                    throw new FreeCapValidationException("sitekey is required for CaptchaFox");
                if (string.IsNullOrWhiteSpace(task.SiteUrl))
                    throw new FreeCapValidationException("siteurl is required for CaptchaFox");
                break;

            case CaptchaType.DiscordId:
                if (string.IsNullOrWhiteSpace(task.SiteKey))
                    throw new FreeCapValidationException("sitekey is required for Discord ID");
                if (string.IsNullOrWhiteSpace(task.SiteUrl))
                    throw new FreeCapValidationException("siteurl is required for Discord ID");
                break;

            case CaptchaType.Geetest:
                if (string.IsNullOrWhiteSpace(task.Challenge))
                    throw new FreeCapValidationException("challenge is required for Geetest");
                break;

            case CaptchaType.FunCaptcha:
                if (!task.Preset.HasValue)
                    throw new FreeCapValidationException("preset is required for FunCaptcha");
                if (task.ChromeVersion != "136" && task.ChromeVersion != "137")
                    throw new FreeCapValidationException("chrome_version must be 136 or 137 for FunCaptcha");
                break;

            case CaptchaType.AuroNetwork:
                // No specific validation required
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(captchaType), captchaType, null);
        }
    }

    /// <summary>
    /// Builds the API payload for a captcha task.
    /// </summary>
    /// <param name="task">The captcha task configuration.</param>
    /// <param name="captchaType">The type of captcha being solved.</param>
    /// <returns>A dictionary containing the API payload.</returns>
    private Dictionary<string, object> BuildPayload(CaptchaTask task, CaptchaType captchaType)
    {
        ValidateTask(task, captchaType);

        var payloadData = new Dictionary<string, object>();

        switch (captchaType)
        {
            case CaptchaType.HCaptcha:
                payloadData["websiteURL"] = task.SiteUrl!;
                payloadData["websiteKey"] = task.SiteKey!;
                payloadData["rqData"] = task.RqData!;
                payloadData["groqApiKey"] = task.GroqApiKey!;
                break;

            case CaptchaType.CaptchaFox:
                payloadData["websiteURL"] = task.SiteUrl!;
                payloadData["websiteKey"] = task.SiteKey!;
                break;

            case CaptchaType.Geetest:
                payloadData["Challenge"] = task.Challenge!;
                payloadData["RiskType"] = task.RiskType?.ToApiString() ?? RiskType.Slide.ToApiString();
                break;

            case CaptchaType.DiscordId:
                payloadData["websiteURL"] = task.SiteUrl!;
                payloadData["websiteKey"] = task.SiteKey!;
                break;

            case CaptchaType.FunCaptcha:
                payloadData["preset"] = task.Preset!.Value.ToApiString();
                payloadData["chrome_version"] = task.ChromeVersion;
                payloadData["blob"] = task.Blob;
                break;

            case CaptchaType.AuroNetwork:
                // No specific payload data
                break;
        }

        if (!string.IsNullOrWhiteSpace(task.Proxy))
            payloadData["proxy"] = task.Proxy;

        return new Dictionary<string, object>
        {
            ["captchaType"] = captchaType.ToApiString(),
            ["payload"] = payloadData
        };
    }

    /// <summary>
    /// Makes an HTTP request to the FreeCap API with retry logic.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="data">Optional data to send with the request.</param>
    /// <param name="maxRetries">Maximum number of retries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data as a dictionary.</returns>
    private async Task<Dictionary<string, object>> MakeRequestAsync(
        string method,
        string endpoint,
        Dictionary<string, object>? data = null,
        int? maxRetries = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FreeCapClient));

        maxRetries ??= _config.MaxRetries;
        var url = $"{_config.ApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Making {Method} request to {Url} (attempt {Attempt})", method, url, attempt + 1);

                HttpResponseMessage response;
                if (method.ToUpperInvariant() == "POST" && data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await _httpClient.PostAsync(url, content, cancellationToken);
                }
                else
                {
                    response = await _httpClient.GetAsync(url, cancellationToken);
                }

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                Dictionary<string, object> responseData;

                try
                {
                    responseData = string.IsNullOrEmpty(responseText) 
                        ? new Dictionary<string, object>() 
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(responseText) ?? new Dictionary<string, object>();
                }
                catch (JsonException)
                {
                    responseData = new Dictionary<string, object> { ["raw_response"] = responseText };
                }

                if (response.IsSuccessStatusCode)
                    return responseData;

                var statusCode = (int)response.StatusCode;
                switch (statusCode)
                {
                    case 401:
                        throw new FreeCapApiException("Invalid API key", statusCode, responseData);
                    case 429:
                        throw new FreeCapApiException("Rate limit exceeded", statusCode, responseData);
                    case >= 500:
                        var errorMsg = $"Server error {statusCode}: {responseText}";
                        _logger.LogWarning("{ErrorMessage} (attempt {Attempt})", errorMsg, attempt + 1);
                        lastException = new FreeCapApiException(errorMsg, statusCode, responseData);
                        break;
                    default:
                        throw new FreeCapApiException($"HTTP error {statusCode}: {responseText}", statusCode, responseData);
                }
            }
            catch (HttpRequestException ex)
            {
                var errorMsg = $"Network error: {ex.Message}";
                _logger.LogWarning("{ErrorMessage} (attempt {Attempt})", errorMsg, attempt + 1);
                lastException = new FreeCapApiException(errorMsg);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var errorMsg = $"Request timeout: {ex.Message}";
                _logger.LogWarning("{ErrorMessage} (attempt {Attempt})", errorMsg, attempt + 1);
                lastException = new FreeCapApiException(errorMsg);
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(_config.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt));
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new FreeCapApiException("Max retries exceeded");
    }

    /// <summary>
    /// Create a captcha solving task.
    /// </summary>
    /// <param name="task">Captcha task configuration</param>
    /// <param name="captchaType">Type of captcha to solve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task ID string</returns>
    public async Task<string> CreateTaskAsync(
        CaptchaTask task, 
        CaptchaType captchaType,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildPayload(task, captchaType);

        _logger.LogInformation("Creating {CaptchaType} task for {SiteUrl}", captchaType.ToApiString(), task.SiteUrl ?? "N/A");
        _logger.LogDebug("Task payload: {Payload}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        var response = await MakeRequestAsync("POST", "/CreateTask", payload, cancellationToken: cancellationToken);

        if (!GetBooleanValue(response, "status"))
        {
            var errorMsg = GetStringValue(response, "error") ?? "Unknown error creating task";
            throw new FreeCapApiException($"Failed to create task: {errorMsg}", responseData: response);
        }

        var taskId = GetStringValue(response, "taskId");
        if (string.IsNullOrEmpty(taskId))
            throw new FreeCapApiException("No task ID in response", responseData: response);

        _logger.LogInformation("Task created successfully: {TaskId}", taskId);
        return taskId;
    }

    /// <summary>
    /// Get task result by ID.
    /// </summary>
    /// <param name="taskId">Task ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task result dictionary</returns>
    public async Task<Dictionary<string, object>> GetTaskResultAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskId))
            throw new FreeCapValidationException("Task ID cannot be empty");

        var payload = new Dictionary<string, object> { ["taskId"] = taskId.Trim() };

        _logger.LogDebug("Checking task status: {TaskId}", taskId);

        return await MakeRequestAsync("POST", "/GetTask", payload, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Solve a captcha and return the solution.
    /// </summary>
    /// <param name="task">Captcha task configuration</param>
    /// <param name="captchaType">Type of captcha to solve</param>
    /// <param name="timeout">Maximum time to wait for solution</param>
    /// <param name="checkInterval">Time between status checks</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Captcha solution string</returns>
    public async Task<string> SolveCaptchaAsync(
        CaptchaTask task,
        CaptchaType captchaType,
        TimeSpan? timeout = null,
        TimeSpan? checkInterval = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= _config.DefaultTaskTimeout;
        checkInterval ??= _config.DefaultCheckInterval;

        if (timeout.Value <= TimeSpan.Zero)
            throw new FreeCapValidationException("Timeout must be positive");
        if (checkInterval.Value <= TimeSpan.Zero)
            throw new FreeCapValidationException("Check interval must be positive");

        var taskId = await CreateTaskAsync(task, captchaType, cancellationToken);

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Waiting for task {TaskId} to complete (timeout: {Timeout}s)", taskId, timeout.Value.TotalSeconds);

        using var timeoutCts = new CancellationTokenSource(timeout.Value);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            while (!combinedCts.Token.IsCancellationRequested)
            {
                var elapsedTime = DateTime.UtcNow - startTime;
                var remainingTime = timeout.Value - elapsedTime;

                try
                {
                    var result = await GetTaskResultAsync(taskId, combinedCts.Token);
                    var statusString = GetStringValue(result, "status") ?? "";
                    var status = EnumExtensions.ParseTaskStatus(statusString);

                    _logger.LogDebug("Task {TaskId} status: {Status}", taskId, statusString);

                    switch (status)
                    {
                        case Enums.TaskStatus.Solved:
                            var solution = GetStringValue(result, "solution");
                            if (string.IsNullOrEmpty(solution))
                                throw new FreeCapApiException($"Task {taskId} marked as solved but no solution provided", responseData: result);

                            _logger.LogInformation("Task {TaskId} solved successfully", taskId);
                            return solution;

                        case Enums.TaskStatus.Error:
                        case Enums.TaskStatus.Failed:
                            var errorMessage = GetStringValue(result, "error") ?? GetStringValue(result, "Error") ?? "Unknown error";
                            throw new FreeCapApiException($"Task {taskId} failed: {errorMessage}", responseData: result);

                        case Enums.TaskStatus.Processing:
                        case Enums.TaskStatus.Pending:
                            _logger.LogDebug("Task {TaskId} still {Status}, {RemainingTime:F0}s remaining", taskId, statusString, remainingTime.TotalSeconds);
                            break;

                        default:
                            _logger.LogWarning("Unknown task status for {TaskId}: {Status}", taskId, statusString);
                            break;
                    }
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    throw new FreeCapTimeoutException($"Task {taskId} timed out after {timeout.Value.TotalSeconds} seconds");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error checking task {TaskId}: {Error}", taskId, ex.Message);
                }

                await Task.Delay(checkInterval.Value, combinedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new FreeCapTimeoutException($"Task {taskId} timed out after {timeout.Value.TotalSeconds} seconds");
        }

        throw new FreeCapTimeoutException($"Task {taskId} timed out after {timeout.Value.TotalSeconds} seconds");
    }

    /// <summary>
    /// Gets a string value from a dictionary response.
    /// </summary>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The string value or null if not found.</returns>
    private static string? GetStringValue(Dictionary<string, object> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Gets a boolean value from a dictionary response.
    /// </summary>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to look for.</param>
    /// <returns>The boolean value or false if not found or cannot be parsed.</returns>
    private static bool GetBooleanValue(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value)) return false;
        
        return value switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var result) && result,
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            _ => false
        };
    }

    /// <summary>
    /// Disposes the client resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the client resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose implementation.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
            _logger.LogDebug("Client disposed");
        }
    }

    /// <summary>
    /// Protected async dispose implementation.
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
            _logger.LogDebug("Client disposed");
        }
        await Task.CompletedTask;
    }
} 