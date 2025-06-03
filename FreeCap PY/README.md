# FreeCap Python Client

A robust, production-ready async client for the FreeCap captcha solving service. Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.

[![PyPI version](https://badge.fury.io/py/freecap-client.svg)](https://badge.fury.io/py/freecap-client)
[![Python versions](https://img.shields.io/pypi/pyversions/freecap-client.svg)](https://pypi.org/project/freecap-client/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

## Features

- 🚀 **Async/await support** - Built with modern Python async patterns
- 🛡️ **Robust error handling** - Comprehensive exception handling and retries
- 🎯 **Multiple captcha types** - hCaptcha, FunCaptcha, Geetest, Discord ID, and more
- 🔧 **Highly configurable** - Customizable timeouts, retries, and logging
- 📝 **Type hints** - Full type annotation support
- 🧪 **Production ready** - Thoroughly tested and battle-tested

## Supported Captcha Types

- **hCaptcha** - Most popular captcha service
- **FunCaptcha** - Interactive puzzle captchas
- **Geetest** - Behavioral captcha verification
- **CaptchaFox** - Alternative captcha service
- **Discord ID** - Discord-specific captcha solving
- **Auro Network** - Specialized network captchas

## Installation

```bash
pip install freecap-client
```

For development dependencies:
```bash
pip install freecap-client[dev]
```

## Quick Start

### Basic Usage

```python
import asyncio
from freecap import FreeCapClient, CaptchaTask, CaptchaType

async def main():
    client = FreeCapClient(api_key="your-api-key")
    
    # Solve hCaptcha
    task = CaptchaTask(
        sitekey="your-sitekey",
        siteurl="https://example.com",
        rqdata="your-rqdata",
        groq_api_key="your-groq-key"
    )
    
    solution = await client.solve_captcha(task, CaptchaType.HCAPTCHA)
    print(f"Solution: {solution}")
    
    await client.close()

asyncio.run(main())
```

### Using Context Manager

```python
import asyncio
from freecap import FreeCapClient, CaptchaTask, CaptchaType

async def main():
    async with FreeCapClient(api_key="your-api-key") as client:
        task = CaptchaTask(
            sitekey="your-sitekey",
            siteurl="https://example.com",
            rqdata="your-rqdata",
            groq_api_key="your-groq-key"
        )
        
        solution = await client.solve_captcha(task, CaptchaType.HCAPTCHA)
        print(f"Solution: {solution}")

asyncio.run(main())
```

### Convenience Functions

For simple use cases, you can use the convenience functions:

```python
import asyncio
from freecap import solve_hcaptcha, solve_funcaptcha, FunCaptchaPreset

async def main():
    # Solve hCaptcha
    solution = await solve_hcaptcha(
        api_key="your-api-key",
        sitekey="your-sitekey",
        siteurl="https://example.com",
        rqdata="your-rqdata",
        groq_api_key="your-groq-key"
    )
    
    # Solve FunCaptcha
    solution = await solve_funcaptcha(
        api_key="your-api-key",
        preset=FunCaptchaPreset.ROBLOX_LOGIN
    )

asyncio.run(main())
```

## Advanced Usage

### Custom Configuration

```python
from freecap import FreeCapClient, ClientConfig, ConsoleLogger
import logging

config = ClientConfig(
    api_url="https://freecap.su",
    request_timeout=30,
    max_retries=3,
    retry_delay=1.0,
    default_task_timeout=120,
    default_check_interval=3
)

logger = ConsoleLogger(level=logging.DEBUG)
client = FreeCapClient(api_key="your-api-key", config=config, logger=logger)
```

### Different Captcha Types

```python
from freecap import CaptchaTask, CaptchaType, RiskType, FunCaptchaPreset

# Geetest
geetest_task = CaptchaTask(
    challenge="your-challenge",
    risk_type=RiskType.SLIDE
)

# FunCaptcha
funcaptcha_task = CaptchaTask(
    preset=FunCaptchaPreset.ROBLOX_LOGIN,
    chrome_version="137",
    blob="undefined"
)

# CaptchaFox
captchafox_task = CaptchaTask(
    sitekey="your-sitekey",
    siteurl="https://example.com"
)
```

### Error Handling

```python
from freecap import (
    FreeCapClient, 
    FreeCapAPIException, 
    FreeCapTimeoutException,
    FreeCapValidationException
)

async def solve_with_error_handling():
    try:
        async with FreeCapClient(api_key="your-api-key") as client:
            solution = await client.solve_captcha(task, CaptchaType.HCAPTCHA)
            return solution
    except FreeCapAPIException as e:
        print(f"API Error: {e}, Status: {e.status_code}")
    except FreeCapTimeoutException as e:
        print(f"Timeout Error: {e}")
    except FreeCapValidationException as e:
        print(f"Validation Error: {e}")
```

## API Reference

### FreeCapClient

Main client class for interacting with the FreeCap API.

#### Methods

- `solve_captcha(task, captcha_type, timeout=None, check_interval=None)` - Solve a captcha
- `create_task(task, captcha_type)` - Create a new captcha task
- `get_task_result(task_id)` - Get the result of a task
- `close()` - Close the client session

### CaptchaTask

Configuration for captcha solving tasks.

#### Fields

- `sitekey` - Site key for the captcha
- `siteurl` - URL where the captcha is located
- `proxy` - Proxy to use (optional)
- `rqdata` - Request data for hCaptcha
- `groq_api_key` - Groq API key for hCaptcha
- `challenge` - Challenge for Geetest
- `risk_type` - Risk type for Geetest
- `preset` - Preset for FunCaptcha
- `chrome_version` - Chrome version for FunCaptcha
- `blob` - Blob data for FunCaptcha

### Enums

- `CaptchaType` - Supported captcha types
- `TaskStatus` - Task status values
- `RiskType` - Geetest risk types
- `FunCaptchaPreset` - FunCaptcha presets

## Requirements

- Python 3.10+
- aiohttp 3.8.0+

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Support

- Documentation: https://freecap.su/docs
- Issues: https://github.com/freecap/freecap-python-client/issues
- Website: https://freecap.su

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

### 1.0.0
- Initial release
- Support for all major captcha types
- Async/await support
- Comprehensive error handling
- Type hints and documentation 