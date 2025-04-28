use reqwest::header::{HeaderMap, HeaderValue, CONTENT_TYPE};
use serde::{Deserialize, Serialize};
use std::time::{Duration, Instant};
use thiserror::Error;

#[derive(Debug, Clone)]
pub struct CaptchaTask {
    pub sitekey: String,
    pub siteurl: String,
    pub proxy: String,
    pub rqdata: Option<String>,
}

#[derive(Debug, Serialize)]
struct TaskPayload {
    sitekey: String,
    siteurl: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    proxy: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    rqdata: Option<String>,
}

#[derive(Debug, Serialize)]
struct CreateTaskRequest {
    freecap_key: String,
    captcha_type: String,
    payload: TaskPayload,
}

#[derive(Debug, Serialize)]
struct GetTaskRequest {
    freecap_key: String,
    task_id: String,
}

#[derive(Debug, Deserialize)]
struct CreateTaskResponse {
    success: bool,
    task_id: Option<String>,
    error: Option<String>,
}

#[derive(Debug, Deserialize)]
struct GetTaskResponse {
    status: String,
    captcha_token: Option<String>,
    error: Option<String>,
}

#[derive(Debug, Error)]
pub enum FreecapError {
    #[error("API error: {0}")]
    ApiError(String),
    
    #[error("Request error: {0}")]
    RequestError(#[from] reqwest::Error),
    
    #[error("Task timed out after {0} seconds")]
    Timeout(u64),
}

pub struct FreeCapClient {
    api_key: String,
    api_url: String,
    client: reqwest::Client,
}

impl FreeCapClient {
    pub fn new(api_key: String, api_url: Option<String>) -> Self {
        let api_url = match api_url {
            Some(url) => url.trim_end_matches('/').to_string(),
            None => "https://freecap.app".to_string(),
        };
        
        let client = reqwest::Client::new();
        
        Self {
            api_key,
            api_url,
            client,
        }
    }
    
    pub async fn create_task(
        &self,
        task: &CaptchaTask,
        captcha_type: &str,
    ) -> Result<String, FreecapError> {
        let mut payload = TaskPayload {
            sitekey: task.sitekey.clone(),
            siteurl: task.siteurl.clone(),
            proxy: None,
            rqdata: None,
        };
        
        if !task.proxy.is_empty() {
            payload.proxy = Some(task.proxy.clone());
        }
        
        if captcha_type == "hcaptcha" && task.rqdata.is_some() {
            payload.rqdata = task.rqdata.clone();
        }
        
        let request_data = CreateTaskRequest {
            freecap_key: self.api_key.clone(),
            captcha_type: captcha_type.to_string(),
            payload,
        };
        
        let response = self.client
            .post(format!("{}/create_task", self.api_url))
            .json(&request_data)
            .send()
            .await?;
            
        let result: CreateTaskResponse = response.json().await?;
        
        if !result.success || result.task_id.is_none() {
            return Err(FreecapError::ApiError(
                result.error.unwrap_or_else(|| "Unknown error".to_string())
            ));
        }
        
        Ok(result.task_id.unwrap())
    }
    
    pub async fn get_result(&self, task_id: &str) -> Result<GetTaskResponse, FreecapError> {
        let request_data = GetTaskRequest {
            freecap_key: self.api_key.clone(),
            task_id: task_id.to_string(),
        };
        
        let response = self.client
            .post(format!("{}/get_task", self.api_url))
            .json(&request_data)
            .send()
            .await?;
            
        let result: GetTaskResponse = response.json().await?;
        Ok(result)
    }
    
    pub async fn solve_captcha(
        &self,
        task: &CaptchaTask,
        captcha_type: &str,
        timeout: u64,
        check_interval: u64,
    ) -> Result<String, FreecapError> {
        let task_id = self.create_task(task, captcha_type).await?;
        
        let start_time = Instant::now();
        loop {
            if start_time.elapsed() > Duration::from_secs(timeout) {
                return Err(FreecapError::Timeout(timeout));
            }
            
            let result = self.get_result(&task_id).await?;
            
            match result.status.as_str() {
                "solved" => {
                    return Ok(result.captcha_token.unwrap_or_default());
                }
                "error" => {
                    return Err(FreecapError::ApiError(
                        result.error.unwrap_or_else(|| "Unknown error".to_string())
                    ));
                }
                _ => {
                    tokio::time::sleep(Duration::from_secs(check_interval)).await;
                }
            }
        }
    }
}