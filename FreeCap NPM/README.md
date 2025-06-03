# FreeCap Client

A robust, production-ready async client for the FreeCap captcha solving service. Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.

[![npm version](https://badge.fury.io/js/freecap-client.svg)](https://badge.fury.io/js/freecap-client)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

## Features

- 🚀 **Async/Await Support** - Modern JavaScript with Promise-based API
- 🔧 **Multiple Captcha Types** - hCaptcha, FunCaptcha, Geetest, and more
- 🛡️ **Robust Error Handling** - Comprehensive exception types and retry logic
- 📝 **TypeScript Support** - Full type definitions included
- 🔍 **Configurable Logging** - Built-in logging with multiple levels
- ⚡ **High Performance** - Optimized for speed and reliability
- 🎯 **Easy to Use** - Simple API with convenience functions

## Supported Captcha Types

- **hCaptcha** - Including Discord integration with rqData support
- **FunCaptcha** - With presets for Roblox, Dropbox
- **Geetest** - All risk types (slide, gobang, icon, AI)
- **CaptchaFox** - Advanced captcha solving
- **Discord ID** - Discord-specific captcha handling
- **Auro Network** - Specialized network captchas

## Installation

```bash
npm install freecap-client
```

## Quick Start

### Basic hCaptcha Solving

```javascript
const { solveHCaptcha } = require('freecap-client');

async function solveCaptcha() {
    try {
        const solution = await solveHCaptcha(
            'your-api-key',
            'site-key',
            'https://discord.com',
            'rq-data',
            'groq-api-key'
        );
        console.log('Solution:', solution);
    } catch (error) {
        console.error('Error:', error.message);
    }
}
```

### Using the Main Client

```javascript
const { FreeCapClient, CaptchaTask, CaptchaType } = require('freecap-client');

async function main() {
    const client = new FreeCapClient('your-api-key');
    
    try {
        const task = new CaptchaTask({
            sitekey: 'your-sitekey',
            siteurl: 'https://example.com',
            groq_api_key: 'your-groq-key'
        });
        
        const solution = await client.solveCaptcha(
            task, 
            CaptchaType.HCAPTCHA,
            180 // 3 minute timeout
        );
        
        console.log('Captcha solved:', solution);
    } finally {
        await client.close();
    }
}
```

## API Reference

### FreeCapClient

The main client class for interacting with the FreeCap API.

#### Constructor

```javascript
new FreeCapClient(apiKey, config?, logger?)
```

- `apiKey` (string): Your FreeCap API key
- `config` (ClientConfig, optional): Configuration options
- `logger` (ILogger, optional): Custom logger instance

#### Methods

##### `createTask(task, captchaType)`

Creates a new captcha solving task.

- `task` (CaptchaTask): Task configuration
- `captchaType` (CaptchaType): Type of captcha to solve
- Returns: Promise<string> - Task ID

##### `getTaskResult(taskId)`

Gets the result of a captcha solving task.

- `taskId` (string): Task ID returned from createTask
- Returns: Promise<object> - Task result

##### `solveCaptcha(task, captchaType, timeout?, checkInterval?)`

Solves a captcha end-to-end (creates task and waits for result).

- `task` (CaptchaTask): Task configuration
- `captchaType` (CaptchaType): Type of captcha to solve
- `timeout` (number, optional): Timeout in seconds (default: 120)
- `checkInterval` (number, optional): Check interval in seconds (default: 3)
- Returns: Promise<string> - Captcha solution

##### `close()`

Closes the client and cleans up resources.

### CaptchaTask

Configuration class for captcha tasks.

```javascript
new CaptchaTask({
    sitekey: 'site-key',
    siteurl: 'https://example.com',
    proxy: 'http://user:pass@host:port',
    rqdata: 'rq-data-for-discord',
    groq_api_key: 'groq-api-key',
    // ... other options
})
```

### Configuration Options

#### ClientConfig

```javascript
const { ClientConfig } = require('freecap-client');

const config = new ClientConfig({
    baseUrl: 'https://freecap.su',  // API base URL
    timeout: 30000,                      // Request timeout (ms)
    maxRetries: 3,                       // Max retry attempts
    retryDelay: 1000,                    // Delay between retries (ms)
    defaultTimeout: 120,                 // Default solve timeout (s)
    defaultCheckInterval: 3              // Default check interval (s)
});
```

#### Logging

```javascript
const { ConsoleLogger, NullLogger } = require('freecap-client');

// Console logger with debug level
const logger = new ConsoleLogger('debug');

// Disable logging
const nullLogger = new NullLogger();

const client = new FreeCapClient('api-key', null, logger);
```

## Examples

### Discord hCaptcha

```javascript
const { solveHCaptcha } = require('freecap-client');

const solution = await solveHCaptcha(
    'your-api-key',
    'a9b5fb07-92ff-493f-86fe-352a2803b3df',
    'https://discord.com',
    'your-rq-data',
    'your-groq-api-key'
);
```

### Roblox FunCaptcha

```javascript
const { solveFunCaptcha, FunCaptchaPreset } = require('freecap-client');

const solution = await solveFunCaptcha(
    'your-api-key',
    FunCaptchaPreset.ROBLOX_LOGIN,
    '137', // Chrome version
    'your-blob-data'
);
```

### Geetest Captcha

```javascript
const { FreeCapClient, CaptchaTask, CaptchaType, RiskType } = require('freecap-client');

const client = new FreeCapClient('your-api-key');
const task = new CaptchaTask({
    sitekey: 'your-sitekey',
    siteurl: 'https://example.com',
    challenge: 'challenge-data',
    risk_type: RiskType.SLIDE
});

const solution = await client.solveCaptcha(task, CaptchaType.GEETEST);
```

## Error Handling

The client provides specific exception types for different error scenarios:

```javascript
const { 
    FreeCapException,
    FreeCapAPIException,
    FreeCapTimeoutException,
    FreeCapValidationException 
} = require('freecap-client');

try {
    const solution = await client.solveCaptcha(task, captchaType);
} catch (error) {
    if (error instanceof FreeCapValidationException) {
        console.log('Validation error:', error.message);
    } else if (error instanceof FreeCapTimeoutException) {
        console.log('Timeout error:', error.message);
    } else if (error instanceof FreeCapAPIException) {
        console.log('API error:', error.message);
        console.log('Status code:', error.statusCode);
        console.log('Response:', error.responseData);
    } else {
        console.log('Unexpected error:', error.message);
    }
}
```

## TypeScript Support

This package includes full TypeScript definitions:

```typescript
import { 
    FreeCapClient, 
    CaptchaTask, 
    CaptchaType,
    ClientConfig 
} from 'freecap-client';

const client: FreeCapClient = new FreeCapClient('api-key');
const task: CaptchaTask = new CaptchaTask({
    sitekey: 'key',
    siteurl: 'https://example.com'
});

const solution: string = await client.solveCaptcha(
    task, 
    CaptchaType.HCAPTCHA
);
```

## Constants

### CaptchaType

- `HCAPTCHA` - hCaptcha solving
- `FUNCAPTCHA` - FunCaptcha solving
- `GEETEST` - Geetest captcha solving
- `CAPTCHAFOX` - CaptchaFox solving
- `DISCORD_ID` - Discord ID captcha
- `AURO_NETWORK` - Auro Network captcha

### FunCaptchaPreset

- `ROBLOX_LOGIN` - Roblox login preset
- `ROBLOX_FOLLOW` - Roblox follow preset
- `ROBLOX_GROUP` - Roblox group preset
- `DROPBOX_LOGIN` - Dropbox login preset

### RiskType (for Geetest)

- `SLIDE` - Slide puzzle
- `GOBANG` - Gobang game
- `ICON` - Icon selection
- `AI` - AI-based challenge

## Requirements

- Node.js 20.0.0 or higher
- Valid FreeCap API key
- Internet connection

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

### 1.0.0
- Initial release
- Support for hCaptcha, FunCaptcha, Geetest
- TypeScript definitions
- Comprehensive error handling
- Configurable logging 