import aiohttp
import json
import time
import asyncio
import logging
from typing import Dict, Any, Optional


class CaptchaTask:
    def __init__(self, sitekey: str, siteurl: str, proxy: str, rqdata: str = None):
        self.sitekey = sitekey
        self.siteurl = siteurl
        self.proxy = proxy
        self.rqdata = rqdata


class ILogger:
    def info(self, message: str):
        pass


class ConsoleLogger(ILogger):
    def info(self, message: str):
        print(f"[INFO] {message}")


class FreeCapClient:
    def __init__(self, api_key: str, api_url: str = "https://freecap.app", logger: Optional[ILogger] = None):
        self._api_url = api_url.rstrip('/')
        self._logger = logger or ConsoleLogger()
        self._headers = {"X-API-Key": api_key}
        self._session = None

    async def _ensure_session(self):
        if self._session is None or self._session.closed:
            self._session = aiohttp.ClientSession(headers=self._headers)
        return self._session

    async def create_task_async(self, task: CaptchaTask, captcha_type: str = "hcaptcha") -> Dict[str, Any]:
        task_data = {
            "captchaType": captcha_type,
            "payload": {
                "websiteURL": task.siteurl,
                "websiteKey": task.sitekey
            }
        }
        
        if task.proxy:
            task_data["payload"]["proxy"] = task.proxy
        
        if task.rqdata and captcha_type == "hcaptcha":
            task_data["payload"]["rqdata"] = task.rqdata
        
        self._logger.info(f"Creating {captcha_type} task for site: {task.siteurl}")
        
        session = await self._ensure_session()
        async with session.post(
            f"{self._api_url}/CreateTask",
            json=task_data
        ) as response:
            if response.status != 200:
                error_text = await response.text()
                raise Exception(f"HTTP error {response.status}: {error_text}")
            
            result = await response.json()
            
            if not result.get("status") or "taskId" not in result:
                raise Exception(f"Error creating task: {await response.text()}")
            
            return result
    
    async def get_result_async(self, task_id: str) -> Dict[str, Any]:
        request_data = {"taskId": task_id}
        
        session = await self._ensure_session()
        async with session.post(
            f"{self._api_url}/GetTask",
            json=request_data
        ) as response:
            if response.status != 200:
                error_text = await response.text()
                raise Exception(f"HTTP error {response.status}: {error_text}")
            
            return await response.json()
    
    async def solve_captcha_async(
        self,
        task: CaptchaTask,
        captcha_type: str = "hcaptcha",
        timeout: int = 120,
        check_interval: int = 3
    ) -> str:
        task_result = await self.create_task_async(task, captcha_type)
        task_id = task_result.get("taskId") or task_result.get("task_id")
        
        if not task_id:
            raise ValueError(f"Invalid task result: {task_result}")
        
        start_time = time.time()
        while True:
            if time.time() - start_time > timeout:
                raise TimeoutError(f"Task {task_id} timed out after {timeout} seconds")
            
            result = await self.get_result_async(task_id)
            
            if result["status"] == "Solved":
                self._logger.info(f"Task {task_id} solved successfully")
                return result["solution"]
            elif result["status"] == "Error":
                error_message = result.get("Error", "Unknown error")
                raise Exception(f"Task {task_id} failed: {error_message}")
            
            await asyncio.sleep(check_interval)
    
    async def close(self):
        """Close the aiohttp session when done with the client."""
        if self._session and not self._session.closed:
            await self._session.close()
