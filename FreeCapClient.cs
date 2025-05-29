using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FreeCapClient
{
    /// <summary>
    /// FreeCap API Client - Professional C# Implementation
    /// 
    /// A robust, production-ready async client for the FreeCap captcha solving service.
    /// Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.
    /// 
    /// Author: FreeCap Client
    /// Version: 1.0.0
    /// License: GPLv3
    /// </summary>

    #region Enums

    public enum CaptchaType
    {
        HCaptcha,
        CaptchaFox,
        Geetest,
        DiscordId,
        FunCaptcha,
        AuroNetwork
    }

    public enum TaskStatus
    {
        Pending,
        Processing,
        Solved,
        Error,
        Failed
    }

    public enum RiskType
    {
        Slide,
        Gobang,
        Icon,
        Ai
    }

    public enum FunCaptchaPreset
    {
        SnapchatLogin,
        RobloxLogin,
        RobloxFollow,
        RobloxGroup,
        DropboxLogin
    }

    #endregion

    #region Extensions

    public static class EnumExtensions
    {
        public static string ToApiString(this CaptchaType captchaType)
        {
            return captchaType switch
            {
                CaptchaType.HCaptcha => "hcaptcha",
                CaptchaType.CaptchaFox => "captchafox",
                CaptchaType.Geetest => "geetest",
                CaptchaType.DiscordId => "discordid",
                CaptchaType.FunCaptcha => "funcaptcha",
                CaptchaType.AuroNetwork => "auronetwork",
                _ => throw new ArgumentOutOfRangeException(nameof(captchaType), captchaType, null)
            };
        }

        public static string ToApiString(this TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Pending => "pending",
                TaskStatus.Processing => "processing",
                TaskStatus.Solved => "solved",
                TaskStatus.Error => "error",
                TaskStatus.Failed => "failed",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }

        public static string ToApiString(this RiskType riskType)
        {
            return riskType switch
            {
                RiskType.Slide => "slide",
                RiskType.Gobang => "gobang",
                RiskType.Icon => "icon",
                RiskType.Ai => "ai",
                _ => throw new ArgumentOutOfRangeException(nameof(riskType), riskType, null)
            };
        }

        public static string ToApiString(this FunCaptchaPreset preset)
        {
            return preset switch
            {
                FunCaptchaPreset.SnapchatLogin => "snapchat_login",
                FunCaptchaPreset.RobloxLogin => "roblox_login",
                FunCaptchaPreset.RobloxFollow => "roblox_follow",
                FunCaptchaPreset.RobloxGroup => "roblox_group",
                FunCaptchaPreset.DropboxLogin => "dropbox_login",
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
            };
        }

        public static TaskStatus ParseTaskStatus(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => TaskStatus.Pending,
                "processing" => TaskStatus.Processing,
                "solved" => TaskStatus.Solved,
                "error" => TaskStatus.Error,
                "failed" => TaskStatus.Failed,
                _ => TaskStatus.Error
            };
        }
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Captcha task configuration.
    /// 
    /// Different captcha types require different fields:
    /// - hCaptcha: sitekey, siteurl, rqdata, groq_api_key (required)
    /// - CaptchaFox: sitekey, siteurl
    /// - Geetest: challenge, risk_type
    /// - Discord ID: sitekey, siteurl
    /// - FunCaptcha: preset, chrome_version, blob
    /// - Auro Network: no specific fields required
    /// </summary>
    public class CaptchaTask
    {
        public string? SiteKey { get; set; }
        public string? SiteUrl { get; set; }
        public string? Proxy { get; set; }
        
        // hCaptcha specific
        public string? RqData { get; set; }
        public string? GroqApiKey { get; set; }
        
        // Geetest specific
        public string? Challenge { get; set; }
        public RiskType? RiskType { get; set; }
        
        // FunCaptcha specific
        public FunCaptchaPreset? Preset { get; set; }
        public string ChromeVersion { get; set; } = "137";
        public string Blob { get; set; } = "undefined";
    }

    /// <summary>
    /// Client configuration options.
    /// </summary>
    public class ClientConfig
    {
        public string ApiUrl { get; set; } = "https://freecap.su";
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan DefaultTaskTimeout { get; set; } = TimeSpan.FromSeconds(120);
        public TimeSpan DefaultCheckInterval { get; set; } = TimeSpan.FromSeconds(3);
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";
    }

    #endregion

    #region Exceptions

    public class FreeCapException : Exception
    {
        public FreeCapException(string message) : base(message) { }
        public FreeCapException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class FreeCapApiException : FreeCapException
    {
        public int? StatusCode { get; }
        public Dictionary<string, object>? ResponseData { get; }

        public FreeCapApiException(string message, int? statusCode = null, Dictionary<string, object>? responseData = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseData = responseData;
        }
    }

    public class FreeCapTimeoutException : FreeCapException
    {
        public FreeCapTimeoutException(string message) : base(message) { }
    }

    public class FreeCapValidationException : FreeCapException
    {
        public FreeCapValidationException(string message) : base(message) { }
    }

    #endregion

    #region Main Client

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

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = _config.RequestTimeout;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

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
                            case TaskStatus.Solved:
                                var solution = GetStringValue(result, "solution");
                                if (string.IsNullOrEmpty(solution))
                                    throw new FreeCapApiException($"Task {taskId} marked as solved but no solution provided", responseData: result);

                                _logger.LogInformation("Task {TaskId} solved successfully", taskId);
                                return solution;

                            case TaskStatus.Error:
                            case TaskStatus.Failed:
                                var errorMessage = GetStringValue(result, "error") ?? GetStringValue(result, "Error") ?? "Unknown error";
                                throw new FreeCapApiException($"Task {taskId} failed: {errorMessage}", responseData: result);

                            case TaskStatus.Processing:
                            case TaskStatus.Pending:
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

        private static string? GetStringValue(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
                _logger.LogDebug("Client disposed");
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
                _logger.LogDebug("Client disposed");
            }
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Static convenience methods for common captcha solving scenarios.
    /// </summary>
    public static class FreeCapSolver
    {
        /// <summary>
        /// Convenience method to solve hCaptcha.
        /// </summary>
        public static async Task<string> SolveHCaptchaAsync(
            string apiKey,
            string siteKey,
            string siteUrl,
            string rqData,
            string groqApiKey,
            string? proxy = null,
            TimeSpan? timeout = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            using var client = new FreeCapClient(apiKey, logger: logger);
            
            var task = new CaptchaTask
            {
                SiteKey = siteKey,
                SiteUrl = siteUrl,
                RqData = rqData,
                GroqApiKey = groqApiKey,
                Proxy = proxy
            };

            return await client.SolveCaptchaAsync(
                task, 
                CaptchaType.HCaptcha, 
                timeout ?? TimeSpan.FromSeconds(120),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Convenience method to solve FunCaptcha.
        /// </summary>
        public static async Task<string> SolveFunCaptchaAsync(
            string apiKey,
            FunCaptchaPreset preset,
            string chromeVersion = "137",
            string blob = "undefined",
            string? proxy = null,
            TimeSpan? timeout = null,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            using var client = new FreeCapClient(apiKey, logger: logger);
            
            var task = new CaptchaTask
            {
                Preset = preset,
                ChromeVersion = chromeVersion,
                Blob = blob,
                Proxy = proxy
            };

            return await client.SolveCaptchaAsync(
                task, 
                CaptchaType.FunCaptcha, 
                timeout ?? TimeSpan.FromSeconds(120),
                cancellationToken: cancellationToken);
        }
    }

    #endregion

    #region Example Usage

    /// <summary>
    /// Example program demonstrating FreeCap client usage.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure logging
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<FreeCapClient>();

            try
            {
                // Using the client with using statement for proper disposal
                using var client = new FreeCapClient("your-api-key", logger: logger);
                
                var task = new CaptchaTask
                {
                    SiteKey = "a9b5fb07-92ff-493f-86fe-352a2803b3df",
                    SiteUrl = "discord.com",
                    RqData = "your-rq-data-here",
                    GroqApiKey = "your-groq-api-key",
                    Proxy = "http://user:pass@host:port"
                };

                var solution = await client.SolveCaptchaAsync(
                    task: task,
                    captchaType: CaptchaType.HCaptcha,
                    timeout: TimeSpan.FromSeconds(180));

                Console.WriteLine($"✅ hCaptcha solved: {solution}");

                // Example using convenience method
                var solution2 = await FreeCapSolver.SolveHCaptchaAsync(
                    apiKey: "your-api-key",
                    siteKey: "a9b5fb07-92ff-493f-86fe-352a2803b3df",
                    siteUrl: "discord.com",
                    rqData: "your-rq-data-here",
                    groqApiKey: "your-groq-api-key",
                    logger: logger);

                Console.WriteLine($"✅ hCaptcha solved (convenience method): {solution2}");
            }
            catch (FreeCapValidationException ex)
            {
                Console.WriteLine($"❌ Validation error: {ex.Message}");
            }
            catch (FreeCapTimeoutException ex)
            {
                Console.WriteLine($"⏰ Timeout error: {ex.Message}");
            }
            catch (FreeCapApiException ex)
            {
                Console.WriteLine($"🌐 API error: {ex.Message}");
                if (ex.StatusCode.HasValue)
                    Console.WriteLine($"   Status code: {ex.StatusCode}");
                if (ex.ResponseData != null)
                    Console.WriteLine($"   Response: {JsonSerializer.Serialize(ex.ResponseData, new JsonSerializerOptions { WriteIndented = true })}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Unexpected error: {ex.Message}");
            }
        }
    }

    #endregion
}
