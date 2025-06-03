using FreeCap.Client.Enums;

namespace FreeCap.Client.Extensions;

/// <summary>
/// Extension methods for enum types used in the FreeCap client.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Converts a CaptchaType enum value to its corresponding API string representation.
    /// </summary>
    /// <param name="captchaType">The captcha type to convert.</param>
    /// <returns>The API string representation of the captcha type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the captcha type is not recognized.</exception>
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

    /// <summary>
    /// Converts a TaskStatus enum value to its corresponding API string representation.
    /// </summary>
    /// <param name="status">The task status to convert.</param>
    /// <returns>The API string representation of the task status.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the task status is not recognized.</exception>
    public static string ToApiString(this Enums.TaskStatus status)
    {
        return status switch
        {
            Enums.TaskStatus.Pending => "pending",
            Enums.TaskStatus.Processing => "processing",
            Enums.TaskStatus.Solved => "solved",
            Enums.TaskStatus.Error => "error",
            Enums.TaskStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    /// <summary>
    /// Converts a RiskType enum value to its corresponding API string representation.
    /// </summary>
    /// <param name="riskType">The risk type to convert.</param>
    /// <returns>The API string representation of the risk type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the risk type is not recognized.</exception>
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

    /// <summary>
    /// Converts a FunCaptchaPreset enum value to its corresponding API string representation.
    /// </summary>
    /// <param name="preset">The FunCaptcha preset to convert.</param>
    /// <returns>The API string representation of the preset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the preset is not recognized.</exception>
    public static string ToApiString(this FunCaptchaPreset preset)
    {
        return preset switch
        {
            FunCaptchaPreset.RobloxLogin => "roblox_login",
            FunCaptchaPreset.RobloxFollow => "roblox_follow",
            FunCaptchaPreset.RobloxGroup => "roblox_group",
            FunCaptchaPreset.DropboxLogin => "dropbox_login",
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }

    /// <summary>
    /// Parses a string status value into a TaskStatus enum value.
    /// </summary>
    /// <param name="status">The string status to parse.</param>
    /// <returns>The corresponding TaskStatus enum value, or TaskStatus.Error if parsing fails.</returns>
    public static Enums.TaskStatus ParseTaskStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" => Enums.TaskStatus.Pending,
            "processing" => Enums.TaskStatus.Processing,
            "solved" => Enums.TaskStatus.Solved,
            "error" => Enums.TaskStatus.Error,
            "failed" => Enums.TaskStatus.Failed,
            _ => Enums.TaskStatus.Error
        };
    }
} 