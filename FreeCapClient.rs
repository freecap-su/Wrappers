//! FreeCap API Client - Professional Rust Implementation
//!
//! A robust, production-ready async client for the FreeCap captcha solving service.
//! Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.
//!
//! # Example
//! ```rust
//! use freecap_client::*;
//!
//! #[tokio::main]
//! async fn main() -> Result<(), Box<dyn std::error::Error>> {
//!     let client = FreeCapClient::new("your-api-key".to_string())?;
//!     
//!     let task = CaptchaTask::builder()
//!         .sitekey("your-sitekey")
//!         .siteurl("discord.com")
//!         .rqdata("your-rqdata")
//!         .groq_api_key("your-groq-key")
//!         .build();
//!     
//!     let solution = client.solve_captcha(task, CaptchaType::HCaptcha, None, None).await?;
//!     println!("Solution: {}", solution);
//!     Ok(())
//! }
//! ```

use reqwest::{Client as HttpClient, Response};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fmt;
use std::time::{Duration, Instant};
use thiserror::Error;
use tokio::time::sleep;
use tracing::{debug, error, info, warn};

/// Supported captcha types
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CaptchaType {
    #[serde(rename = "hcaptcha")]
    HCaptcha,
    #[serde(rename = "captchafox")]
    CaptchaFox,
    #[serde(rename = "geetest")]
    Geetest,
    #[serde(rename = "discordid")]
    DiscordId,
    #[serde(rename = "funcaptcha")]
    FunCaptcha,
    #[serde(rename = "auronetwork")]
    AuroNetwork,
}

impl fmt::Display for CaptchaType {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let s = match self {
            CaptchaType::HCaptcha => "hcaptcha",
            CaptchaType::CaptchaFox => "captchafox",
            CaptchaType::Geetest => "geetest",
            CaptchaType::DiscordId => "discordid",
            CaptchaType::FunCaptcha => "funcaptcha",
            CaptchaType::AuroNetwork => "auronetwork",
        };
        write!(f, "{}", s)
    }
}

/// Task status values
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum TaskStatus {
    Pending,
    Processing,
    Solved,
    Error,
    Failed,
}

/// Geetest risk types
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum RiskType {
    Slide,
    Gobang,
    Icon,
    Ai,
}

impl fmt::Display for RiskType {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let s = match self {
            RiskType::Slide => "slide",
            RiskType::Gobang => "gobang",
            RiskType::Icon => "icon",
            RiskType::Ai => "ai",
        };
        write!(f, "{}", s)
    }
}

/// FunCaptcha presets
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub enum FunCaptchaPreset {
    SnapchatLogin,
    RobloxLogin,
    RobloxFollow,
    RobloxGroup,
    DropboxLogin,
}

impl fmt::Display for FunCaptchaPreset {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let s = match self {
            FunCaptchaPreset::SnapchatLogin => "snapchat_login",
            FunCaptchaPreset::RobloxLogin => "roblox_login",
            FunCaptchaPreset::RobloxFollow => "roblox_follow",
            FunCaptchaPreset::RobloxGroup => "roblox_group",
            FunCaptchaPreset::DropboxLogin => "dropbox_login",
        };
        write!(f, "{}", s)
    }
}

/// Captcha task configuration
#[derive(Debug, Clone, Default)]
pub struct CaptchaTask {
    pub sitekey: Option<String>,
    pub siteurl: Option<String>,
    pub proxy: Option<String>,
    pub rqdata: Option<String>,
    pub groq_api_key: Option<String>,
    pub challenge: Option<String>,
    pub risk_type: Option<RiskType>,
    pub preset: Option<FunCaptchaPreset>,
    pub chrome_version: Option<String>,
    pub blob: Option<String>,
}

impl CaptchaTask {
    /// Create a new builder for CaptchaTask
    pub fn builder() -> CaptchaTaskBuilder {
        CaptchaTaskBuilder::default()
    }
}

/// Builder for CaptchaTask
#[derive(Debug, Clone, Default)]
pub struct CaptchaTaskBuilder {
    task: CaptchaTask,
}

impl CaptchaTaskBuilder {
    pub fn sitekey<S: Into<String>>(mut self, sitekey: S) -> Self {
        self.task.sitekey = Some(sitekey.into());
        self
    }

    pub fn siteurl<S: Into<String>>(mut self, siteurl: S) -> Self {
        self.task.siteurl = Some(siteurl.into());
        self
    }

    pub fn proxy<S: Into<String>>(mut self, proxy: S) -> Self {
        self.task.proxy = Some(proxy.into());
        self
    }

    pub fn rqdata<S: Into<String>>(mut self, rqdata: S) -> Self {
        self.task.rqdata = Some(rqdata.into());
        self
    }

    pub fn groq_api_key<S: Into<String>>(mut self, groq_api_key: S) -> Self {
        self.task.groq_api_key = Some(groq_api_key.into());
        self
    }

    pub fn challenge<S: Into<String>>(mut self, challenge: S) -> Self {
        self.task.challenge = Some(challenge.into());
        self
    }

    pub fn risk_type(mut self, risk_type: RiskType) -> Self {
        self.task.risk_type = Some(risk_type);
        self
    }

    pub fn preset(mut self, preset: FunCaptchaPreset) -> Self {
        self.task.preset = Some(preset);
        self
    }

    pub fn chrome_version<S: Into<String>>(mut self, chrome_version: S) -> Self {
        self.task.chrome_version = Some(chrome_version.into());
        self
    }

    pub fn blob<S: Into<String>>(mut self, blob: S) -> Self {
        self.task.blob = Some(blob.into());
        self
    }

    pub fn build(self) -> CaptchaTask {
        self.task
    }
}

/// FreeCap client errors
#[derive(Error, Debug)]
pub enum FreeCapError {
    #[error("Validation error: {0}")]
    Validation(String),
    
    #[error("API error (status: {status:?}): {message}")]
    Api {
        message: String,
        status: Option<u16>,
        response_data: Option<serde_json::Value>,
    },
    
    #[error("Task timed out after {seconds} seconds")]
    Timeout { seconds: u64 },
    
    #[error("HTTP error: {0}")]
    Http(#[from] reqwest::Error),
    
    #[error("JSON error: {0}")]
    Json(#[from] serde_json::Error),
    
    #[error("Client error: {0}")]
    Client(String),
}

/// Client configuration options
#[derive(Debug, Clone)]
pub struct ClientConfig {
    pub api_url: String,
    pub request_timeout: Duration,
    pub max_retries: u32,
    pub retry_delay: Duration,
    pub default_task_timeout: Duration,
    pub default_check_interval: Duration,
    pub user_agent: String,
}

impl Default for ClientConfig {
    fn default() -> Self {
        Self {
            api_url: "https://freecap.su".to_string(),
            request_timeout: Duration::from_secs(30),
            max_retries: 3,
            retry_delay: Duration::from_secs(1),
            default_task_timeout: Duration::from_secs(120),
            default_check_interval: Duration::from_secs(3),
            user_agent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36".to_string(),
        }
    }
}

/// API response structures
#[derive(Debug, Deserialize)]
struct CreateTaskResponse {
    status: bool,
    #[serde(rename = "taskId")]
    task_id: Option<String>,
    error: Option<String>,
}

#[derive(Debug, Deserialize)]
struct GetTaskResponse {
    status: Option<String>,
    solution: Option<String>,
    error: Option<String>,
    #[serde(rename = "Error")]
    error_alt: Option<String>,
}

/// Professional async client for FreeCap captcha solving service
pub struct FreeCapClient {
    api_key: String,
    config: ClientConfig,
    http_client: HttpClient,
    api_url: String,
}

impl FreeCapClient {
    /// Create a new FreeCap client
    pub fn new(api_key: String) -> Result<Self, FreeCapError> {
        Self::with_config(api_key, ClientConfig::default())
    }

    /// Create a new FreeCap client with custom configuration
    pub fn with_config(api_key: String, config: ClientConfig) -> Result<Self, FreeCapError> {
        if api_key.trim().is_empty() {
            return Err(FreeCapError::Validation("API key cannot be empty".to_string()));
        }

        if !config.api_url.starts_with("http://") && !config.api_url.starts_with("https://") {
            return Err(FreeCapError::Validation(
                "API URL must start with http:// or https://".to_string(),
            ));
        }

        let mut headers = reqwest::header::HeaderMap::new();
        headers.insert("X-API-Key", api_key.trim().parse().map_err(|_| {
            FreeCapError::Validation("Invalid API key format".to_string())
        })?);
        headers.insert("Content-Type", "application/json".parse().unwrap());
        headers.insert("User-Agent", config.user_agent.parse().unwrap());
        headers.insert("Accept", "application/json".parse().unwrap());

        let http_client = HttpClient::builder()
            .timeout(config.request_timeout)
            .default_headers(headers)
            .build()?;

        let api_url = config.api_url.trim_end_matches('/').to_string();

        Ok(Self {
            api_key: api_key.trim().to_string(),
            config,
            http_client,
            api_url,
        })
    }

    /// Validate task configuration for specific captcha type
    fn validate_task(&self, task: &CaptchaTask, captcha_type: CaptchaType) -> Result<(), FreeCapError> {
        match captcha_type {
            CaptchaType::HCaptcha => {
                if task.sitekey.is_none() {
                    return Err(FreeCapError::Validation("sitekey is required for hCaptcha".to_string()));
                }
                if task.siteurl.is_none() {
                    return Err(FreeCapError::Validation("siteurl is required for hCaptcha".to_string()));
                }
                if task.groq_api_key.is_none() {
                    return Err(FreeCapError::Validation("groq_api_key is required for hCaptcha".to_string()));
                }
                if task.rqdata.is_none() {
                    return Err(FreeCapError::Validation("rqdata cannot be blank for Discord hCaptcha".to_string()));
                }
            }
            CaptchaType::CaptchaFox => {
                if task.sitekey.is_none() {
                    return Err(FreeCapError::Validation("sitekey is required for CaptchaFox".to_string()));
                }
                if task.siteurl.is_none() {
                    return Err(FreeCapError::Validation("siteurl is required for CaptchaFox".to_string()));
                }
            }
            CaptchaType::DiscordId => {
                if task.sitekey.is_none() {
                    return Err(FreeCapError::Validation("sitekey is required for Discord ID".to_string()));
                }
                if task.siteurl.is_none() {
                    return Err(FreeCapError::Validation("siteurl is required for Discord ID".to_string()));
                }
            }
            CaptchaType::Geetest => {
                if task.challenge.is_none() {
                    return Err(FreeCapError::Validation("challenge is required for Geetest".to_string()));
                }
            }
            CaptchaType::FunCaptcha => {
                if task.preset.is_none() {
                    return Err(FreeCapError::Validation("preset is required for FunCaptcha".to_string()));
                }
                if let Some(ref version) = task.chrome_version {
                    if version != "136" && version != "137" {
                        return Err(FreeCapError::Validation(
                            "chrome_version must be 136 or 137 for FunCaptcha".to_string(),
                        ));
                    }
                }
            }
            CaptchaType::AuroNetwork => {
                // No specific validation required
            }
        }
        Ok(())
    }

    /// Build API payload for specific captcha type
    fn build_payload(&self, task: &CaptchaTask, captcha_type: CaptchaType) -> Result<serde_json::Value, FreeCapError> {
        self.validate_task(task, captcha_type)?;

        let mut payload_data = serde_json::Map::new();

        match captcha_type {
            CaptchaType::HCaptcha => {
                payload_data.insert("websiteURL".to_string(), task.siteurl.as_ref().unwrap().clone().into());
                payload_data.insert("websiteKey".to_string(), task.sitekey.as_ref().unwrap().clone().into());
                payload_data.insert("rqData".to_string(), task.rqdata.as_ref().unwrap().clone().into());
                payload_data.insert("groqApiKey".to_string(), task.groq_api_key.as_ref().unwrap().clone().into());
            }
            CaptchaType::CaptchaFox => {
                payload_data.insert("websiteURL".to_string(), task.siteurl.as_ref().unwrap().clone().into());
                payload_data.insert("websiteKey".to_string(), task.sitekey.as_ref().unwrap().clone().into());
            }
            CaptchaType::Geetest => {
                payload_data.insert("Challenge".to_string(), task.challenge.as_ref().unwrap().clone().into());
                let risk_type = task.risk_type.unwrap_or(RiskType::Slide);
                payload_data.insert("RiskType".to_string(), risk_type.to_string().into());
            }
            CaptchaType::DiscordId => {
                payload_data.insert("websiteURL".to_string(), task.siteurl.as_ref().unwrap().clone().into());
                payload_data.insert("websiteKey".to_string(), task.sitekey.as_ref().unwrap().clone().into());
            }
            CaptchaType::FunCaptcha => {
                payload_data.insert("preset".to_string(), task.preset.as_ref().unwrap().to_string().into());
                let chrome_version = task.chrome_version.as_deref().unwrap_or("137");
                payload_data.insert("chrome_version".to_string(), chrome_version.into());
                let blob = task.blob.as_deref().unwrap_or("undefined");
                payload_data.insert("blob".to_string(), blob.into());
            }
            CaptchaType::AuroNetwork => {
                // Empty payload for AuroNetwork
            }
        }

        if let Some(ref proxy) = task.proxy {
            payload_data.insert("proxy".to_string(), proxy.clone().into());
        }

        let payload = serde_json::json!({
            "captchaType": captcha_type.to_string(),
            "payload": payload_data
        });

        Ok(payload)
    }

    /// Make HTTP request with retries
    async fn make_request(&self, method: reqwest::Method, endpoint: &str, data: Option<serde_json::Value>) -> Result<serde_json::Value, FreeCapError> {
        let url = format!("{}/{}", self.api_url, endpoint.trim_start_matches('/'));
        let mut last_error = None;

        for attempt in 0..=self.config.max_retries {
            debug!("Making {} request to {} (attempt {})", method, url, attempt + 1);

            let mut request = self.http_client.request(method.clone(), &url);
            
            if let Some(ref json_data) = data {
                request = request.json(json_data);
            }

            match request.send().await {
                Ok(response) => {
                    let status = response.status();
                    let response_text = response.text().await?;

                    let response_data: serde_json::Value = serde_json::from_str(&response_text)
                        .unwrap_or_else(|_| serde_json::json!({"raw_response": response_text}));

                    if status.is_success() {
                        return Ok(response_data);
                    }

                    let error_msg = match status.as_u16() {
                        401 => "Invalid API key".to_string(),
                        429 => "Rate limit exceeded".to_string(),
                        code if code >= 500 => {
                            let msg = format!("Server error {}: {}", code, response_text);
                            warn!("{} (attempt {})", msg, attempt + 1);
                            last_error = Some(FreeCapError::Api {
                                message: msg,
                                status: Some(code),
                                response_data: Some(response_data),
                            });
                            
                            if attempt < self.config.max_retries {
                                let delay = self.config.retry_delay * 2_u32.pow(attempt);
                                sleep(delay).await;
                                continue;
                            }
                            
                            return Err(last_error.unwrap());
                        }
                        _ => format!("HTTP error {}: {}", status, response_text),
                    };

                    return Err(FreeCapError::Api {
                        message: error_msg,
                        status: Some(status.as_u16()),
                        response_data: Some(response_data),
                    });
                }
                Err(e) => {
                    let error_msg = format!("Network error: {}", e);
                    warn!("{} (attempt {})", error_msg, attempt + 1);
                    last_error = Some(FreeCapError::Http(e));

                    if attempt < self.config.max_retries {
                        let delay = self.config.retry_delay * 2_u32.pow(attempt);
                        sleep(delay).await;
                    }
                }
            }
        }

        Err(last_error.unwrap_or_else(|| FreeCapError::Client("Max retries exceeded".to_string())))
    }

    /// Create a captcha solving task
    pub async fn create_task(&self, task: &CaptchaTask, captcha_type: CaptchaType) -> Result<String, FreeCapError> {
        let payload = self.build_payload(task, captcha_type)?;
        
        info!("Creating {} task for {}", captcha_type, task.siteurl.as_deref().unwrap_or("N/A"));
        debug!("Task payload: {}", serde_json::to_string_pretty(&payload)?);

        let response = self.make_request(reqwest::Method::POST, "/CreateTask", Some(payload)).await?;
        
        let create_response: CreateTaskResponse = serde_json::from_value(response.clone())?;
        
        if !create_response.status {
            let error_msg = create_response.error.unwrap_or_else(|| "Unknown error creating task".to_string());
            return Err(FreeCapError::Api {
                message: format!("Failed to create task: {}", error_msg),
                status: None,
                response_data: Some(response),
            });
        }

        let task_id = create_response.task_id.ok_or_else(|| FreeCapError::Api {
            message: "No task ID in response".to_string(),
            status: None,
            response_data: Some(response),
        })?;

        info!("Task created successfully: {}", task_id);
        Ok(task_id)
    }

    /// Get task result by ID
    pub async fn get_task_result(&self, task_id: &str) -> Result<GetTaskResponse, FreeCapError> {
        if task_id.trim().is_empty() {
            return Err(FreeCapError::Validation("Task ID cannot be empty".to_string()));
        }

        let payload = serde_json::json!({"taskId": task_id.trim()});
        debug!("Checking task status: {}", task_id);

        let response = self.make_request(reqwest::Method::POST, "/GetTask", Some(payload)).await?;
        let task_response: GetTaskResponse = serde_json::from_value(response)?;
        
        Ok(task_response)
    }

    /// Solve a captcha and return the solution
    pub async fn solve_captcha(
        &self,
        task: CaptchaTask,
        captcha_type: CaptchaType,
        timeout: Option<Duration>,
        check_interval: Option<Duration>,
    ) -> Result<String, FreeCapError> {
        let timeout = timeout.unwrap_or(self.config.default_task_timeout);
        let check_interval = check_interval.unwrap_or(self.config.default_check_interval);

        if timeout.is_zero() {
            return Err(FreeCapError::Validation("Timeout must be positive".to_string()));
        }
        if check_interval.is_zero() {
            return Err(FreeCapError::Validation("Check interval must be positive".to_string()));
        }

        let task_id = self.create_task(&task, captcha_type).await?;
        let start_time = Instant::now();
        
        info!("Waiting for task {} to complete (timeout: {}s)", task_id, timeout.as_secs());

        loop {
            let elapsed = start_time.elapsed();
            if elapsed >= timeout {
                return Err(FreeCapError::Timeout {
                    seconds: timeout.as_secs(),
                });
            }

            match self.get_task_result(&task_id).await {
                Ok(result) => {
                    let status = result.status.as_deref().unwrap_or("").to_lowercase();
                    debug!("Task {} status: {}", task_id, status);

                    match status.as_str() {
                        "solved" => {
                            let solution = result.solution.ok_or_else(|| FreeCapError::Api {
                                message: format!("Task {} marked as solved but no solution provided", task_id),
                                status: None,
                                response_data: None,
                            })?;
                            
                            info!("Task {} solved successfully", task_id);
                            return Ok(solution);
                        }
                        "error" | "failed" => {
                            let error_message = result.error
                                .or(result.error_alt)
                                .unwrap_or_else(|| "Unknown error".to_string());
                            
                            return Err(FreeCapError::Api {
                                message: format!("Task {} failed: {}", task_id, error_message),
                                status: None,
                                response_data: None,
                            });
                        }
                        "processing" | "pending" => {
                            let remaining = timeout.saturating_sub(elapsed);
                            debug!("Task {} still {}, {}s remaining", task_id, status, remaining.as_secs());
                        }
                        _ => {
                            warn!("Unknown task status for {}: {}", task_id, status);
                        }
                    }
                }
                Err(e) => {
                    warn!("Error checking task {}: {}", task_id, e);
                }
            }

            sleep(check_interval).await;
        }
    }
}

/// Convenience function to solve hCaptcha
pub async fn solve_hcaptcha(
    api_key: String,
    sitekey: String,
    siteurl: String,
    rqdata: String,
    groq_api_key: String,
    proxy: Option<String>,
    timeout: Option<Duration>,
) -> Result<String, FreeCapError> {
    let client = FreeCapClient::new(api_key)?;
    
    let task = CaptchaTask::builder()
        .sitekey(sitekey)
        .siteurl(siteurl)
        .rqdata(rqdata)
        .groq_api_key(groq_api_key)
        .proxy(proxy.unwrap_or_default())
        .build();

    client.solve_captcha(task, CaptchaType::HCaptcha, timeout, None).await
}

/// Convenience function to solve FunCaptcha
pub async fn solve_funcaptcha(
    api_key: String,
    preset: FunCaptchaPreset,
    chrome_version: Option<String>,
    blob: Option<String>,
    proxy: Option<String>,
    timeout: Option<Duration>,
) -> Result<String, FreeCapError> {
    let client = FreeCapClient::new(api_key)?;
    
    let mut task_builder = CaptchaTask::builder().preset(preset);
    
    if let Some(cv) = chrome_version {
        task_builder = task_builder.chrome_version(cv);
    }
    if let Some(b) = blob {
        task_builder = task_builder.blob(b);
    }
    if let Some(p) = proxy {
        task_builder = task_builder.proxy(p);
    }
    
    let task = task_builder.build();
    client.solve_captcha(task, CaptchaType::FunCaptcha, timeout, None).await
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_captcha_task_builder() {
        let task = CaptchaTask::builder()
            .sitekey("test-key")
            .siteurl("discord.com")
            .rqdata("test-rqdata")
            .groq_api_key("test-groq-key")
            .build();

        assert_eq!(task.sitekey, Some("test-key".to_string()));
        assert_eq!(task.siteurl, Some("discord.com".to_string()));
        assert_eq!(task.rqdata, Some("test-rqdata".to_string()));
        assert_eq!(task.groq_api_key, Some("test-groq-key".to_string()));
    }

    #[test]
    fn test_client_creation() {
        let client = FreeCapClient::new("test-api-key".to_string());
        assert!(client.is_ok());

        let empty_key_client = FreeCapClient::new("".to_string());
        assert!(matches!(empty_key_client, Err(FreeCapError::Validation(_))));
    }
}

// Example usage
#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Initialize tracing
    tracing_subscriber::fmt::init();

    // Example: Solve hCaptcha
    let client = FreeCapClient::new("your-api-key".to_string())?;
    
    let task = CaptchaTask::builder()
        .sitekey("a9b5fb07-92ff-493f-86fe-352a2803b3df")
        .siteurl("discord.com")
        .rqdata("your-rq-data-here")
        .groq_api_key("your-groq-api-key")
        .proxy("http://user:pass@host:port")
        .build();
    
    match client.solve_captcha(
        task,
        CaptchaType::HCaptcha,
        Some(Duration::from_secs(180)),
        None,
    ).await {
        Ok(solution) => println!("✅ hCaptcha solved: {}", solution),
        Err(FreeCapError::Validation(e)) => println!("❌ Validation error: {}", e),
        Err(FreeCapError::Timeout { seconds }) => println!("⏰ Timeout error: {} seconds", seconds),
        Err(FreeCapError::Api { message, status, .. }) => {
            println!("🌐 API error: {}", message);
            if let Some(code) = status {
                println!("   Status code: {}", code);
            }
        }
        Err(e) => println!("💥 Unexpected error: {}", e),
    }

    Ok(())
}
