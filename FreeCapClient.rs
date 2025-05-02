use reqwest::{Client, header};
use serde::{Deserialize, Serialize};
use std::time::{Duration, Instant};
use tokio::time;
use anyhow::{Result, anyhow};

pub struct CaptchaTask {
    pub sitekey: String,
    pub siteurl: String,
    pub proxy: String,
    pub rqdata: Option<String>,
}

pub trait ILogger {
    fn info(&self, message: &str);
}

pub struct ConsoleLogger;

impl ILogger for ConsoleLogger {
    fn info(&self, message: &str) {
        println!("[INFO] {}", message);
    }
}

#[derive(Serialize)]
struct TaskPayload {
    websiteURL: String,
    websiteKey: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    proxy: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    rqdata: Option<String>,
}

#[derive(Serialize)]
struct CreateTaskRequest {
    captchaType: String,
    payload: TaskPayload,
}

#[derive(Deserialize, Debug)]
struct CreateTaskResponse {
    status: Option<bool>,
    taskId: Option<String>,
}

#[derive(Serialize)]
struct GetTaskRequest {
    taskId: String,
}

#[derive(Deserialize, Debug)]
struct GetTaskResponse {
    status: String,
    solution: Option<String>,
    #[serde(rename = "Error")]
    error: Option<String>,
}

pub struct FreeCapClient {
    api_url: String,
    client: Client,
    logger: Box<dyn ILogger>,
}

impl FreeCapClient {
    pub fn new(api_key: &str, api_url: Option<&str>, logger: Option<Box<dyn ILogger>>) -> Result<Self> {
        let mut headers = header::HeaderMap::new();
        headers.insert(
            "X-API-Key",
            header::HeaderValue::from_str(api_key)
                .map_err(|e| anyhow!("Invalid API key: {}", e))?,
        );

        let client = Client::builder()
            .default_headers(headers)
            .build()
            .map_err(|e| anyhow!("Failed to create HTTP client: {}", e))?;

        let api_url = match api_url {
            Some(url) => url.trim_end_matches('/').to_string(),
            None => "https://freecap.app".to_string(),
        };

        let logger = logger.unwrap_or_else(|| Box::new(ConsoleLogger));

        Ok(Self {
            api_url,
            client,
            logger,
        })
    }

    pub async fn create_task_async(&self, task: &CaptchaTask, captcha_type: &str) -> Result<CreateTaskResponse> {
        let proxy = if task.proxy.is_empty() {
            None
        } else {
            Some(task.proxy.clone())
        };

        let rqdata = if captcha_type == "hcaptcha" {
            task.rqdata.clone()
        } else {
            None
        };

        let task_data = CreateTaskRequest {
            captchaType: captcha_type.to_string(),
            payload: TaskPayload {
                websiteURL: task.siteurl.clone(),
                websiteKey: task.sitekey.clone(),
                proxy,
                rqdata,
            },
        };

        self.logger.info(&format!(
            "Creating {} task for site: {}",
            captcha_type, task.siteurl
        ));

        let response = self
            .client
            .post(&format!("{}/CreateTask", self.api_url))
            .json(&task_data)
            .send()
            .await
            .map_err(|e| anyhow!("HTTP request failed: {}", e))?;

        if !response.status().is_success() {
            let error_text = response
                .text()
                .await
                .unwrap_or_else(|_| "Failed to get error text".to_string());
            return Err(anyhow!(
                "HTTP error {}: {}",
                response.status(),
                error_text
            ));
        }

        let result: CreateTaskResponse = response
            .json()
            .await
            .map_err(|e| anyhow!("Failed to parse response: {}", e))?;

        if result.status.is_none() || result.taskId.is_none() {
            return Err(anyhow!("Error creating task: Invalid response format"));
        }

        Ok(result)
    }

    pub async fn get_result_async(&self, task_id: &str) -> Result<GetTaskResponse> {
        let request_data = GetTaskRequest {
            taskId: task_id.to_string(),
        };

        let response = self
            .client
            .post(&format!("{}/GetTask", self.api_url))
            .json(&request_data)
            .send()
            .await
            .map_err(|e| anyhow!("HTTP request failed: {}", e))?;

        if !response.status().is_success() {
            let error_text = response
                .text()
                .await
                .unwrap_or_else(|_| "Failed to get error text".to_string());
            return Err(anyhow!(
                "HTTP error {}: {}",
                response.status(),
                error_text
            ));
        }

        let result: GetTaskResponse = response
            .json()
            .await
            .map_err(|e| anyhow!("Failed to parse response: {}", e))?;

        Ok(result)
    }

    pub async fn solve_captcha_async(
        &self,
        task: &CaptchaTask,
        captcha_type: &str,
        timeout: u64,
        check_interval: u64,
    ) -> Result<String> {
        let task_result = self.create_task_async(task, captcha_type).await?;
        let task_id = task_result
            .taskId
            .ok_or_else(|| anyhow!("Invalid task result: task ID is missing"))?;

        let start_time = Instant::now();
        loop {
            if start_time.elapsed() > Duration::from_secs(timeout) {
                return Err(anyhow!(
                    "Task {} timed out after {} seconds",
                    task_id,
                    timeout
                ));
            }

            let result = self.get_result_async(&task_id).await?;

            match result.status.as_str() {
                "Solved" => {
                    self.logger.info(&format!("Task {} solved successfully", task_id));
                    return Ok(result
                        .solution
                        .ok_or_else(|| anyhow!("Solution missing from solved task"))?);
                }
                "Error" => {
                    let error_message = result.error.unwrap_or_else(|| "Unknown error".to_string());
                    return Err(anyhow!("Task {} failed: {}", task_id, error_message));
                }
                _ => {
                    time::sleep(Duration::from_secs(check_interval)).await;
                }
            }
        }
    }
}
