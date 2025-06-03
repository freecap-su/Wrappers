using FreeCap.Client.Enums;

namespace FreeCap.Client.Models;

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
    /// <summary>
    /// The site key for the captcha.
    /// </summary>
    public string? SiteKey { get; set; }
    
    /// <summary>
    /// The URL of the site where the captcha is located.
    /// </summary>
    public string? SiteUrl { get; set; }
    
    /// <summary>
    /// Optional proxy to use for solving the captcha.
    /// </summary>
    public string? Proxy { get; set; }
    
    #region hCaptcha specific
    
    /// <summary>
    /// RqData parameter for hCaptcha (required for hCaptcha).
    /// </summary>
    public string? RqData { get; set; }
    
    /// <summary>
    /// Groq API key for hCaptcha (required for hCaptcha).
    /// </summary>
    public string? GroqApiKey { get; set; }
    
    #endregion
    
    #region Geetest specific
    
    /// <summary>
    /// Challenge parameter for Geetest.
    /// </summary>
    public string? Challenge { get; set; }
    
    /// <summary>
    /// Risk type for Geetest challenges.
    /// </summary>
    public RiskType? RiskType { get; set; }
    
    #endregion
    
    #region FunCaptcha specific
    
    /// <summary>
    /// Preset configuration for FunCaptcha.
    /// </summary>
    public FunCaptchaPreset? Preset { get; set; }
    
    /// <summary>
    /// Chrome version to use for FunCaptcha. Defaults to "137".
    /// </summary>
    public string ChromeVersion { get; set; } = "137";
    
    /// <summary>
    /// Blob parameter for FunCaptcha. Defaults to "undefined".
    /// </summary>
    public string Blob { get; set; } = "undefined";
    
    #endregion
} 