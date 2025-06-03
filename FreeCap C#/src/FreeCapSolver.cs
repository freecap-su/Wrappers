using Microsoft.Extensions.Logging;
using FreeCap.Client.Enums;
using FreeCap.Client.Models;

namespace FreeCap.Client;

/// <summary>
/// Static convenience methods for common captcha solving scenarios.
/// </summary>
public static class FreeCapSolver
{
    /// <summary>
    /// Convenience method to solve hCaptcha.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="siteKey">The site key for hCaptcha.</param>
    /// <param name="siteUrl">The URL where the captcha is located.</param>
    /// <param name="rqData">The rqData parameter for hCaptcha.</param>
    /// <param name="groqApiKey">Your Groq API key.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
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
    /// Convenience method to solve CaptchaFox.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="siteKey">The site key for CaptchaFox.</param>
    /// <param name="siteUrl">The URL where the captcha is located.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
    public static async Task<string> SolveCaptchaFoxAsync(
        string apiKey,
        string siteKey,
        string siteUrl,
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
            Proxy = proxy
        };

        return await client.SolveCaptchaAsync(
            task, 
            CaptchaType.CaptchaFox, 
            timeout ?? TimeSpan.FromSeconds(120),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Convenience method to solve FunCaptcha.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="preset">The FunCaptcha preset to use.</param>
    /// <param name="chromeVersion">Chrome version to use (136 or 137).</param>
    /// <param name="blob">Blob parameter for FunCaptcha.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
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

    /// <summary>
    /// Convenience method to solve Geetest.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="challenge">The challenge parameter for Geetest.</param>
    /// <param name="riskType">The risk type for Geetest.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
    public static async Task<string> SolveGeetestAsync(
        string apiKey,
        string challenge,
        RiskType riskType = RiskType.Slide,
        string? proxy = null,
        TimeSpan? timeout = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        using var client = new FreeCapClient(apiKey, logger: logger);
        
        var task = new CaptchaTask
        {
            Challenge = challenge,
            RiskType = riskType,
            Proxy = proxy
        };

        return await client.SolveCaptchaAsync(
            task, 
            CaptchaType.Geetest, 
            timeout ?? TimeSpan.FromSeconds(120),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Convenience method to solve Discord ID verification.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="siteKey">The site key for Discord ID.</param>
    /// <param name="siteUrl">The URL where the captcha is located.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
    public static async Task<string> SolveDiscordIdAsync(
        string apiKey,
        string siteKey,
        string siteUrl,
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
            Proxy = proxy
        };

        return await client.SolveCaptchaAsync(
            task, 
            CaptchaType.DiscordId, 
            timeout ?? TimeSpan.FromSeconds(120),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Convenience method to solve Auro Network captcha.
    /// </summary>
    /// <param name="apiKey">Your FreeCap API key.</param>
    /// <param name="proxy">Optional proxy to use.</param>
    /// <param name="timeout">Maximum time to wait for solution.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captcha solution string.</returns>
    public static async Task<string> SolveAuroNetworkAsync(
        string apiKey,
        string? proxy = null,
        TimeSpan? timeout = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        using var client = new FreeCapClient(apiKey, logger: logger);
        
        var task = new CaptchaTask
        {
            Proxy = proxy
        };

        return await client.SolveCaptchaAsync(
            task, 
            CaptchaType.AuroNetwork, 
            timeout ?? TimeSpan.FromSeconds(120),
            cancellationToken: cancellationToken);
    }
} 