import requests
import time
from typing import Optional, Literal, Dict, Any
from dataclasses import dataclass
from loguru import logger

@dataclass
class CaptchaTask:
    sitekey: str
    siteurl: str
    proxy: str
    rqdata: Optional[str] = None

class FreeCapClient:
    def __init__(self, api_key: str, api_url: str = "https://freecap.app"):
        self.api_key = api_key
        self.api_url = api_url.rstrip('/')
        self.logger = logger
        
    def create_task(
        self, 
        task: CaptchaTask, 
        captcha_type: Literal["hcaptcha", "captchafox"] = "hcaptcha"
    ) -> Dict[str, Any]:
        task_data = {
            "freecap_key": self.api_key,
            "captcha_type": captcha_type,
            "payload": {
                "sitekey": task.sitekey,
                "siteurl": task.siteurl
            }
        }
        
        if task.proxy:
            task_data["payload"]["proxy"] = task.proxy
            
        if task.rqdata and captcha_type == "hcaptcha":
            task_data["payload"]["rqdata"] = task.rqdata
            
        self.logger.info(f"Creating {captcha_type} task for site: {task.siteurl}")
        response = requests.post(f"{self.api_url}/create_task", json=task_data)
        response.raise_for_status()
        
        result = response.json()
        if not result.get("success") or "task_id" not in result:
            raise Exception(f"Error creating task: {result}")
            
        return result
        
    def get_result(self, task_id: str) -> Dict[str, Any]:
        response = requests.post(
            f"{self.api_url}/get_task",
            json={
                "freecap_key": self.api_key,
                "task_id": task_id
            }
        )
        response.raise_for_status()
        return response.json()
        
    def solve_captcha(
        self, 
        task: CaptchaTask, 
        captcha_type: Literal["hcaptcha", "captchafox"] = "hcaptcha",
        timeout: int = 120,
        check_interval: int = 3
    ) -> str:
        task_result = self.create_task(task, captcha_type)
        task_id = task_result["task_id"]
        
        start_time = time.time()
        while True:
            if time.time() - start_time > timeout:
                raise TimeoutError(f"Task {task_id} timed out after {timeout} seconds")
                
            result = self.get_result(task_id)
            
            if result["status"] == "solved":
                self.logger.info(f"Task {task_id} solved successfully")
                return result["captcha_token"]
            elif result["status"] == "error":
                raise Exception(f"Task {task_id} failed: {result.get('error', 'Unknown error')}")
                
            time.sleep(check_interval)