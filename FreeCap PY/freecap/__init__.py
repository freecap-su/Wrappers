"""
FreeCap API Client - Professional Python Implementation

A robust, production-ready async client for the FreeCap captcha solving service.
Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.

Author: FreeCap Client
Version: 1.0.2
License: GPLv3
"""

import aiohttp
import asyncio
import json
import time
from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from enum import Enum
from typing import Any, Dict, Optional, Union, List
import logging
import sys
from contextlib import asynccontextmanager


class CaptchaType(Enum):
    """Supported captcha types."""
    HCAPTCHA = "hcaptcha"
    CAPTCHAFOX = "captchafox"
    GEETEST = "geetest"
    DISCORD_ID = "discordid"
    FUNCAPTCHA = "funcaptcha"


class TaskStatus(Enum):
    """Task status values."""
    PENDING = "pending"
    PROCESSING = "processing"
    SOLVED = "solved"
    ERROR = "error"
    FAILED = "failed"


class RiskType(Enum):
    """Geetest risk types."""
    SLIDE = "slide"
    GOBANG = "gobang"
    ICON = "icon"
    AI = "ai"


class FunCaptchaPreset(Enum):
    """FunCaptcha presets."""
    ROBLOX_LOGIN = "roblox_login"
    ROBLOX_FOLLOW = "roblox_follow"
    ROBLOX_GROUP = "roblox_group"
    ROBLOX_REGISTER = "roblox_register"
    GITHUB_REGISTER = "github_register"


@dataclass
class CaptchaTask:
    """
    Captcha task configuration.
    
    Different captcha types require different fields:
    - hCaptcha: sitekey, siteurl, rqdata, groq_api_key (required)
    - CaptchaFox: sitekey, siteurl
    - Geetest: challenge, risk_type
    - Discord ID: sitekey, siteurl
    - FunCaptcha: preset, chrome_version, blob
    """
    sitekey: Optional[str] = None
    siteurl: Optional[str] = None
    proxy: Optional[str] = None
    
    rqdata: Optional[str] = None
    groq_api_key: Optional[str] = None
    
    challenge: Optional[str] = None
    risk_type: Optional[Union[str, RiskType]] = None
    
    preset: Optional[Union[str, FunCaptchaPreset]] = None
    chrome_version: Optional[str] = "140"
    blob: Optional[str] = "undefined"
    
    def __post_init__(self):
        """Convert enum values to strings."""
        if isinstance(self.risk_type, RiskType):
            self.risk_type = self.risk_type.value
        if isinstance(self.preset, FunCaptchaPreset):
            self.preset = self.preset.value


class FreeCapException(Exception):
    """Base exception for FreeCap client errors."""
    pass


class FreeCapAPIException(FreeCapException):
    """Exception raised for API-related errors."""
    def __init__(self, message: str, status_code: Optional[int] = None, response_data: Optional[Dict] = None):
        super().__init__(message)
        self.status_code = status_code
        self.response_data = response_data


class FreeCapTimeoutException(FreeCapException):
    """Exception raised when a task times out."""
    pass


class FreeCapValidationException(FreeCapException):
    """Exception raised for validation errors."""
    pass


class ILogger(ABC):
    """Abstract logger interface."""
    
    @abstractmethod
    def debug(self, message: str, **kwargs) -> None:
        pass
    
    @abstractmethod
    def info(self, message: str, **kwargs) -> None:
        pass
    
    @abstractmethod
    def warning(self, message: str, **kwargs) -> None:
        pass
    
    @abstractmethod
    def error(self, message: str, **kwargs) -> None:
        pass


class ConsoleLogger(ILogger):
    """Simple console logger implementation."""
    
    def __init__(self, level: int = logging.INFO):
        self.logger = logging.getLogger("freecap_client")
        if not self.logger.handlers:
            handler = logging.StreamHandler(sys.stdout)
            formatter = logging.Formatter(
                '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
            )
            handler.setFormatter(formatter)
            self.logger.addHandler(handler)
        self.logger.setLevel(level)
    
    def debug(self, message: str, **kwargs) -> None:
        self.logger.debug(message, extra=kwargs)
    
    def info(self, message: str, **kwargs) -> None:
        self.logger.info(message, extra=kwargs)
    
    def warning(self, message: str, **kwargs) -> None:
        self.logger.warning(message, extra=kwargs)
    
    def error(self, message: str, **kwargs) -> None:
        self.logger.error(message, extra=kwargs)


class NullLogger(ILogger):
    """No-op logger that discards all messages."""
    
    def debug(self, message: str, **kwargs) -> None:
        pass
    
    def info(self, message: str, **kwargs) -> None:
        pass
    
    def warning(self, message: str, **kwargs) -> None:
        pass
    
    def error(self, message: str, **kwargs) -> None:
        pass


@dataclass
class ClientConfig:
    """Client configuration options."""
    api_url: str = "https://freecap.su"
    request_timeout: int = 30
    max_retries: int = 3
    retry_delay: float = 1.0
    default_task_timeout: int = 120
    default_check_interval: int = 3
    user_agent: str = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36"


class FreeCapClient:
    """
    Professional async client for FreeCap captcha solving service.
    
    Features:
    - Full support for all captcha types
    - Robust error handling and retries
    - Async context manager support
    - Comprehensive logging
    - Type safety with enums and dataclasses
    - Production-ready configuration options
    
    Example:
        async with FreeCapClient("your-api-key") as client:
            task = CaptchaTask(
                sitekey="your-sitekey",
                siteurl="discord.com",
                rqdata="your-rqdata",
                groq_api_key="your-groq-key"
            )
            solution = await client.solve_captcha(task, CaptchaType.HCAPTCHA)
    """
    
    def __init__(
        self,
        api_key: str,
        config: Optional[ClientConfig] = None,
        logger: Optional[ILogger] = None
    ):
        """
        Initialize the FreeCap client.
        
        Args:
            api_key: Your FreeCap API key
            config: Client configuration options
            logger: Logger instance (defaults to ConsoleLogger)
        """
        if not api_key or not api_key.strip():
            raise FreeCapValidationException("API key cannot be empty")
        
        self._api_key = api_key.strip()
        self._config = config or ClientConfig()
        self._logger = logger or ConsoleLogger()
        self._session: Optional[aiohttp.ClientSession] = None
        self._closed = False
        
        if not self._config.api_url.startswith(('http://', 'https://')):
            raise FreeCapValidationException("API URL must start with http:// or https://")
        
        self._api_url = self._config.api_url.rstrip('/')
        self._headers = {
            "FreeCap-Key": self._api_key,
            "Content-Type": "application/json",
            "User-Agent": self._config.user_agent,
            "Accept": "application/json"
        }
    
    async def _get_session(self) -> aiohttp.ClientSession:
        """Get or create aiohttp session."""
        if self._closed:
            raise FreeCapException("Client has been closed")
        
        if self._session is None or self._session.closed:
            timeout = aiohttp.ClientTimeout(total=self._config.request_timeout)
            connector = aiohttp.TCPConnector(
                limit=100,
                limit_per_host=30,
                ttl_dns_cache=300,
                use_dns_cache=True,
            )
            self._session = aiohttp.ClientSession(
                headers=self._headers,
                timeout=timeout,
                connector=connector,
                raise_for_status=False
            )
        
        return self._session
    
    def _validate_task(self, task: CaptchaTask, captcha_type: CaptchaType) -> None:
        """Validate task configuration for specific captcha type."""
        if captcha_type == CaptchaType.HCAPTCHA:
            if not task.sitekey:
                raise FreeCapValidationException("sitekey is required for hCaptcha")
            if not task.siteurl:
                raise FreeCapValidationException("siteurl is required for hCaptcha")
            if not task.groq_api_key:
                raise FreeCapValidationException("groq_api_key is required for hCaptcha")
            if not task.rqdata:
                raise FreeCapValidationException("rqdata cannot be blank for Discord hCaptcha")
        
        elif captcha_type == CaptchaType.CAPTCHAFOX:
            if not task.sitekey:
                raise FreeCapValidationException("sitekey is required for CaptchaFox")
            if not task.siteurl:
                raise FreeCapValidationException("siteurl is required for CaptchaFox")
        
        elif captcha_type == CaptchaType.DISCORD_ID:
            if not task.sitekey:
                raise FreeCapValidationException("sitekey is required for Discord ID")
            if not task.siteurl:
                raise FreeCapValidationException("siteurl is required for Discord ID")
        
        elif captcha_type == CaptchaType.GEETEST:
            if not task.challenge:
                raise FreeCapValidationException("challenge is required for Geetest")
        
        elif captcha_type == CaptchaType.FUNCAPTCHA:
            if not task.preset:
                raise FreeCapValidationException("preset is required for FunCaptcha")
    
    def _build_payload(self, task: CaptchaTask, captcha_type: CaptchaType) -> Dict[str, Any]:
        """Build API payload for specific captcha type."""
        self._validate_task(task, captcha_type)
        
        payload_data = {}
        
        if captcha_type == CaptchaType.HCAPTCHA:
            payload_data = {
                "websiteURL": task.siteurl,
                "websiteKey": task.sitekey,
                "rqData": task.rqdata,
                "groqApiKey": task.groq_api_key
            }
        
        elif captcha_type == CaptchaType.CAPTCHAFOX:
            payload_data = {
                "websiteURL": task.siteurl,
                "websiteKey": task.sitekey
            }
        
        elif captcha_type == CaptchaType.GEETEST:
            payload_data = {
                "Challenge": task.challenge,
                "RiskType": task.risk_type or RiskType.SLIDE.value
            }
        
        elif captcha_type == CaptchaType.DISCORD_ID:
            payload_data = {
                "websiteURL": task.siteurl,
                "websiteKey": task.sitekey
            }
        
        elif captcha_type == CaptchaType.FUNCAPTCHA:
            payload_data = {
                "preset": task.preset,
                "chrome_version": task.chrome_version,
                "blob": task.blob
            }
        
        if task.proxy:
            payload_data["proxy"] = task.proxy
        
        return {
            "captchaType": captcha_type.value,
            "payload": payload_data
        }
    
    async def _make_request(
        self,
        method: str,
        endpoint: str,
        data: Optional[Dict[str, Any]] = None,
        max_retries: Optional[int] = None
    ) -> Dict[str, Any]:
        """Make HTTP request with retries."""
        if max_retries is None:
            max_retries = self._config.max_retries
        
        session = await self._get_session()
        url = f"{self._api_url}/{endpoint.lstrip('/')}"
        
        last_exception = None
        
        for attempt in range(max_retries + 1):
            try:
                self._logger.debug(f"Making {method} request to {url} (attempt {attempt + 1})")
                
                async with session.request(method, url, json=data) as response:
                    response_text = await response.text()
                    
                    try:
                        response_data = json.loads(response_text) if response_text else {}
                    except json.JSONDecodeError:
                        response_data = {"raw_response": response_text}
                    
                    if response.status == 200:
                        return response_data
                    
                    if response.status == 401:
                        raise FreeCapAPIException(
                            "Invalid API key",
                            status_code=response.status,
                            response_data=response_data
                        )
                    elif response.status == 429:
                        raise FreeCapAPIException(
                            "Rate limit exceeded",
                            status_code=response.status,
                            response_data=response_data
                        )
                    elif response.status >= 500:
                        error_msg = f"Server error {response.status}: {response_text}"
                        self._logger.warning(f"{error_msg} (attempt {attempt + 1})")
                        last_exception = FreeCapAPIException(
                            error_msg,
                            status_code=response.status,
                            response_data=response_data
                        )
                    else:
                        raise FreeCapAPIException(
                            f"HTTP error {response.status}: {response_text}",
                            status_code=response.status,
                            response_data=response_data
                        )
            
            except (aiohttp.ClientError, asyncio.TimeoutError) as e:
                error_msg = f"Network error: {str(e)}"
                self._logger.warning(f"{error_msg} (attempt {attempt + 1})")
                last_exception = FreeCapAPIException(error_msg)
            
            if attempt < max_retries:
                delay = self._config.retry_delay * (2 ** attempt)
                await asyncio.sleep(delay)
        
        raise last_exception or FreeCapAPIException("Max retries exceeded")
    
    async def create_task(self, task: CaptchaTask, captcha_type: CaptchaType) -> str:
        """
        Create a captcha solving task.
        
        Args:
            task: Captcha task configuration
            captcha_type: Type of captcha to solve
            
        Returns:
            Task ID string
            
        Raises:
            FreeCapValidationException: Invalid task configuration
            FreeCapAPIException: API request failed
        """
        payload = self._build_payload(task, captcha_type)
        
        self._logger.info(f"Creating {captcha_type.value} task for {task.siteurl or 'N/A'}")
        self._logger.debug(f"Task payload: {json.dumps(payload, indent=2)}")
        
        response = await self._make_request("POST", "/CreateTask", payload)
        
        if not response.get("status"):
            error_msg = response.get("error", "Unknown error creating task")
            raise FreeCapAPIException(f"Failed to create task: {error_msg}", response_data=response)
        
        task_id = response.get("taskId")
        if not task_id:
            raise FreeCapAPIException("No task ID in response", response_data=response)
        
        self._logger.info(f"Task created successfully: {task_id}")
        return task_id
    
    async def get_task_result(self, task_id: str) -> Dict[str, Any]:
        """
        Get task result by ID.
        
        Args:
            task_id: Task ID to check
            
        Returns:
            Task result dictionary
            
        Raises:
            FreeCapAPIException: API request failed
        """
        if not task_id or not task_id.strip():
            raise FreeCapValidationException("Task ID cannot be empty")
        
        payload = {"taskId": task_id.strip()}
        
        self._logger.debug(f"Checking task status: {task_id}")
        
        response = await self._make_request("POST", "/GetTask", payload)
        return response
    
    async def solve_captcha(
        self,
        task: CaptchaTask,
        captcha_type: CaptchaType,
        timeout: Optional[int] = None,
        check_interval: Optional[int] = None
    ) -> str:
        """
        Solve a captcha and return the solution.
        
        Args:
            task: Captcha task configuration
            captcha_type: Type of captcha to solve
            timeout: Maximum time to wait for solution (seconds)
            check_interval: Time between status checks (seconds)
            
        Returns:
            Captcha solution string
            
        Raises:
            FreeCapValidationException: Invalid parameters
            FreeCapTimeoutException: Task timed out
            FreeCapAPIException: API error occurred
        """
        if timeout is None:
            timeout = self._config.default_task_timeout
        if check_interval is None:
            check_interval = self._config.default_check_interval
        
        if timeout <= 0:
            raise FreeCapValidationException("Timeout must be positive")
        if check_interval <= 0:
            raise FreeCapValidationException("Check interval must be positive")
        
        task_id = await self.create_task(task, captcha_type)
        
        start_time = time.time()
        self._logger.info(f"Waiting for task {task_id} to complete (timeout: {timeout}s)")
        
        while True:
            elapsed_time = time.time() - start_time
            remaining_time = timeout - elapsed_time
            
            if remaining_time <= 0:
                raise FreeCapTimeoutException(f"Task {task_id} timed out after {timeout} seconds")
            
            try:
                result = await self.get_task_result(task_id)
                status = result.get("status", "").lower()
                
                self._logger.debug(f"Task {task_id} status: {status}")
                
                if status == TaskStatus.SOLVED.value:
                    solution = result.get("solution")
                    if not solution:
                        raise FreeCapAPIException(
                            f"Task {task_id} marked as solved but no solution provided",
                            response_data=result
                        )
                    
                    self._logger.info(f"Task {task_id} solved successfully")
                    return solution
                
                elif status in [TaskStatus.ERROR.value, TaskStatus.FAILED.value]:
                    error_message = result.get("error", result.get("Error", "Unknown error"))
                    raise FreeCapAPIException(
                        f"Task {task_id} failed: {error_message}",
                        response_data=result
                    )
                
                elif status in [TaskStatus.PROCESSING.value, TaskStatus.PENDING.value]:
                    self._logger.debug(f"Task {task_id} still {status}, {remaining_time:.0f}s remaining")
                
                else:
                    self._logger.warning(f"Unknown task status for {task_id}: {status}")
            
            except (FreeCapTimeoutException, FreeCapAPIException):
                raise
            except Exception as e:
                self._logger.warning(f"Error checking task {task_id}: {str(e)}")
            
            await asyncio.sleep(check_interval)
    
    async def close(self) -> None:
        """Close the client and cleanup resources."""
        if self._closed:
            return
        
        self._closed = True
        
        if self._session and not self._session.closed:
            await self._session.close()
            self._logger.debug("Client session closed")
    
    async def __aenter__(self):
        """Async context manager entry."""
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        """Async context manager exit."""
        await self.close()
    
    def __del__(self):
        """Destructor to ensure cleanup."""
        if hasattr(self, '_session') and self._session and not self._session.closed:
            try:
                loop = asyncio.get_event_loop()
                if loop.is_running():
                    loop.create_task(self.close())
            except RuntimeError:
                pass


async def solve_hcaptcha(
    api_key: str,
    sitekey: str,
    siteurl: str,
    rqdata: str,
    groq_api_key: str,
    proxy: Optional[str] = None,
    timeout: int = 120
) -> str:
    """
    Convenience function to solve hCaptcha.
    
    Args:
        api_key: FreeCap API key
        sitekey: Site key for hCaptcha
        siteurl: Website URL (should be discord.com for Discord)
        rqdata: rqData parameter (required for Discord)
        groq_api_key: Groq API key for solving
        proxy: Proxy string (optional)
        timeout: Timeout in seconds
        
    Returns:
        Captcha solution
    """
    async with FreeCapClient(api_key) as client:
        task = CaptchaTask(
            sitekey=sitekey,
            siteurl=siteurl,
            rqdata=rqdata,
            groq_api_key=groq_api_key,
            proxy=proxy
        )
        return await client.solve_captcha(task, CaptchaType.HCAPTCHA, timeout=timeout)


async def solve_funcaptcha(
    api_key: str,
    preset: Union[str, FunCaptchaPreset],
    chrome_version: str = "140",
    blob: str = "undefined",
    proxy: Optional[str] = None,
    timeout: int = 120
) -> str:
    """
    Convenience function to solve FunCaptcha.
    
    Args:
        api_key: FreeCap API key
        preset: FunCaptcha preset
        chrome_version: Chrome version
        blob: Blob parameter (required for Roblox presets)
        proxy: Proxy string (optional)
        timeout: Timeout in seconds
        
    Returns:
        Captcha solution
    """
    async with FreeCapClient(api_key) as client:
        task = CaptchaTask(
            preset=preset,
            chrome_version=chrome_version,
            blob=blob,
            proxy=proxy
        )
        return await client.solve_captcha(task, CaptchaType.FUNCAPTCHA, timeout=timeout)


async def main():
    """Example usage of the FreeCap client."""
    logging.basicConfig(level=logging.INFO)
    
    try:
        async with FreeCapClient("your-api-key") as client:
            task = CaptchaTask(
                sitekey="a9b5fb07-92ff-493f-86fe-352a2803b3df",
                siteurl="discord.com",
                rqdata="your-rq-data-here",
                groq_api_key="your-groq-api-key",
                proxy="http://user:pass@host:port"
            )
            
            solution = await client.solve_captcha(
                task=task,
                captcha_type=CaptchaType.HCAPTCHA,
                timeout=180
            )
            
            print(f"✅ hCaptcha solved: {solution}")
    
    except FreeCapValidationException as e:
        print(f"❌ Validation error: {e}")
    except FreeCapTimeoutException as e:
        print(f"⏰ Timeout error: {e}")
    except FreeCapAPIException as e:
        print(f"🌐 API error: {e}")
        if e.status_code:
            print(f"   Status code: {e.status_code}")
        if e.response_data:
            print(f"   Response: {e.response_data}")
    except Exception as e:
        print(f"💥 Unexpected error: {e}")


if __name__ == "__main__":
    asyncio.run(main())
