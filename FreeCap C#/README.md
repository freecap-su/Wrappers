# FreeCap.Client

[![NuGet](https://img.shields.io/nuget/v/FreeCap.Client.svg)](https://www.nuget.org/packages/FreeCap.Client/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

A robust, production-ready async client library for the FreeCap captcha solving service. Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.

## Features

- ✅ **Full Captcha Support**: hCaptcha, FunCaptcha, Geetest, CaptchaFox, Discord ID, Auro Network
- ✅ **Async/Await**: Fully asynchronous with proper cancellation support
- ✅ **Type Safety**: Strong typing with enums and models
- ✅ **Error Handling**: Comprehensive exception handling with retry logic
- ✅ **Logging Support**: Integrated with Microsoft.Extensions.Logging
- ✅ **Resource Management**: Proper IDisposable/IAsyncDisposable implementation
- ✅ **Production Ready**: Configurable timeouts, retries, and HTTP client management
- ✅ **Documentation**: Comprehensive XML documentation for IntelliSense

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package FreeCap.Client
```

Or via Package Manager Console:

```powershell
Install-Package FreeCap.Client
```

## Quick Start

### Basic Usage

```csharp
using FreeCap.Client;
using FreeCap.Client.Models;
using FreeCap.Client.Enums;

// Using the main client
using var client = new FreeCapClient("your-api-key");

var task = new CaptchaTask
{
    SiteKey = "a9b5fb07-92ff-493f-86fe-352a2803b3df",
    SiteUrl = "discord.com",
    RqData = "your-rq-data-here",
    GroqApiKey = "your-groq-api-key"
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.HCaptcha);
Console.WriteLine($"Captcha solved: {solution}");
```

### Convenience Methods

```csharp
using FreeCap.Client;

// Quick hCaptcha solving
var solution = await FreeCapSolver.SolveHCaptchaAsync(
    apiKey: "your-api-key",
    siteKey: "a9b5fb07-92ff-493f-86fe-352a2803b3df",
    siteUrl: "discord.com",
    rqData: "your-rq-data-here",
    groqApiKey: "your-groq-api-key"
);

// Quick FunCaptcha solving
var funcaptchaSolution = await FreeCapSolver.SolveFunCaptchaAsync(
    apiKey: "your-api-key",
    preset: FunCaptchaPreset.RobloxLogin
);
```

## Supported Captcha Types

### hCaptcha
```csharp
var task = new CaptchaTask
{
    SiteKey = "your-site-key",
    SiteUrl = "your-site-url",
    RqData = "your-rq-data",
    GroqApiKey = "your-groq-api-key",
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.HCaptcha);
```

### FunCaptcha
```csharp
var task = new CaptchaTask
{
    Preset = FunCaptchaPreset.RobloxLogin,
    ChromeVersion = "137", // 136 or 137
    Blob = "undefined",
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.FunCaptcha);
```

### Geetest
```csharp
var task = new CaptchaTask
{
    Challenge = "your-challenge",
    RiskType = RiskType.Slide, // Slide, Gobang, Icon, Ai
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.Geetest);
```

### CaptchaFox
```csharp
var task = new CaptchaTask
{
    SiteKey = "your-site-key",
    SiteUrl = "your-site-url",
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.CaptchaFox);
```

### Discord ID
```csharp
var task = new CaptchaTask
{
    SiteKey = "your-site-key",
    SiteUrl = "your-site-url",
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.DiscordId);
```

### Auro Network
```csharp
var task = new CaptchaTask
{
    Proxy = "http://user:pass@host:port" // Optional
};

var solution = await client.SolveCaptchaAsync(task, CaptchaType.AuroNetwork);
```

## Configuration

### Client Configuration
```csharp
var config = new ClientConfig
{
    ApiUrl = "https://freecap.su", // Default API URL
    RequestTimeout = TimeSpan.FromSeconds(30), // HTTP request timeout
    MaxRetries = 3, // Maximum retry attempts
    RetryDelay = TimeSpan.FromSeconds(1), // Base retry delay
    DefaultTaskTimeout = TimeSpan.FromSeconds(120), // Task completion timeout
    DefaultCheckInterval = TimeSpan.FromSeconds(3), // Status check interval
    UserAgent = "Mozilla/5.0..." // Custom User-Agent
};

using var client = new FreeCapClient("your-api-key", config);
```

### Logging Integration
```csharp
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<FreeCapClient>();

using var client = new FreeCapClient("your-api-key", logger: logger);
```

### Custom HttpClient
```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("Custom-Header", "value");

using var client = new FreeCapClient("your-api-key", httpClient: httpClient);
```

## Error Handling

The library provides specific exception types for different error scenarios:

```csharp
try
{
    var solution = await client.SolveCaptchaAsync(task, CaptchaType.HCaptcha);
}
catch (FreeCapValidationException ex)
{
    // Invalid task configuration
    Console.WriteLine($"Validation error: {ex.Message}");
}
catch (FreeCapTimeoutException ex)
{
    // Task timed out
    Console.WriteLine($"Timeout: {ex.Message}");
}
catch (FreeCapApiException ex)
{
    // API error response
    Console.WriteLine($"API error: {ex.Message}");
    if (ex.StatusCode.HasValue)
        Console.WriteLine($"Status code: {ex.StatusCode}");
}
catch (FreeCapException ex)
{
    // Base FreeCap exception
    Console.WriteLine($"FreeCap error: {ex.Message}");
}
```

## Advanced Usage

### Manual Task Management
```csharp
// Create task
var taskId = await client.CreateTaskAsync(task, CaptchaType.HCaptcha);

// Check status manually
var result = await client.GetTaskResultAsync(taskId);

// Poll until completion with custom logic
while (true)
{
    var status = await client.GetTaskResultAsync(taskId);
    // Handle status...
    await Task.Delay(5000); // Custom delay
}
```

### Cancellation Support
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    var solution = await client.SolveCaptchaAsync(
        task, 
        CaptchaType.HCaptcha,
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```

## Target Frameworks

- .NET 8.0
- .NET 9.0

## Dependencies

- Microsoft.Extensions.Logging.Abstractions (≥ 8.0.0)
- System.Text.Json (≥ 8.0.0)
- System.ComponentModel.Annotations (≥ 5.0.0)

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Support

For issues and feature requests, please visit the [GitHub repository](https://github.com/freecap/freecap-client-csharp).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 