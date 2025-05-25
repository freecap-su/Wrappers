// FreeCap API Client - Professional Go Implementation
//
// A robust, production-ready client for the FreeCap captcha solving service.
// Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.
//
// Author: FreeCap Client
// Version: 1.0.0
// License: GPLv3

package main

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"strings"
	"time"
)

// CaptchaType represents supported captcha types
type CaptchaType string

const (
	HCaptcha     CaptchaType = "hcaptcha"
	CaptchaFox   CaptchaType = "captchafox"
	Geetest      CaptchaType = "geetest"
	DiscordID    CaptchaType = "discordid"
	FunCaptcha   CaptchaType = "funcaptcha"
	AuroNetwork  CaptchaType = "auronetwork"
)

// TaskStatus represents task status values
type TaskStatus string

const (
	Pending    TaskStatus = "pending"
	Processing TaskStatus = "processing"
	Solved     TaskStatus = "solved"
	Error      TaskStatus = "error"
	Failed     TaskStatus = "failed"
)

// RiskType represents Geetest risk types
type RiskType string

const (
	Slide  RiskType = "slide"
	Gobang RiskType = "gobang"
	Icon   RiskType = "icon"
	AI     RiskType = "ai"
)

// FunCaptchaPreset represents FunCaptcha presets
type FunCaptchaPreset string

const (
	SnapchatLogin FunCaptchaPreset = "snapchat_login"
	RobloxLogin   FunCaptchaPreset = "roblox_login"
	RobloxFollow  FunCaptchaPreset = "roblox_follow"
	RobloxGroup   FunCaptchaPreset = "roblox_group"
	DropboxLogin  FunCaptchaPreset = "dropbox_login"
)

// CaptchaTask represents captcha task configuration
type CaptchaTask struct {
	// Common fields
	Sitekey string `json:"sitekey,omitempty"`
	Siteurl string `json:"siteurl,omitempty"`
	Proxy   string `json:"proxy,omitempty"`

	// hCaptcha specific
	RqData     string `json:"rqdata,omitempty"`
	GroqAPIKey string `json:"groq_api_key,omitempty"`

	// Geetest specific
	Challenge string   `json:"challenge,omitempty"`
	RiskType  RiskType `json:"risk_type,omitempty"`

	// FunCaptcha specific
	Preset        FunCaptchaPreset `json:"preset,omitempty"`
	ChromeVersion string           `json:"chrome_version,omitempty"`
	Blob          string           `json:"blob,omitempty"`
}

// NewCaptchaTask creates a new CaptchaTask with default values
func NewCaptchaTask() *CaptchaTask {
	return &CaptchaTask{
		ChromeVersion: "137",
		Blob:          "undefined",
		RiskType:      Slide,
	}
}

// Custom error types
type FreeCapError struct {
	Message string
	Type    string
}

func (e *FreeCapError) Error() string {
	return fmt.Sprintf("FreeCap %s: %s", e.Type, e.Message)
}

type FreeCapAPIError struct {
	*FreeCapError
	StatusCode   int
	ResponseData map[string]interface{}
}

func NewFreeCapAPIError(message string, statusCode int, responseData map[string]interface{}) *FreeCapAPIError {
	return &FreeCapAPIError{
		FreeCapError: &FreeCapError{Message: message, Type: "API Error"},
		StatusCode:   statusCode,
		ResponseData: responseData,
	}
}

type FreeCapTimeoutError struct {
	*FreeCapError
}

func NewFreeCapTimeoutError(message string) *FreeCapTimeoutError {
	return &FreeCapTimeoutError{
		FreeCapError: &FreeCapError{Message: message, Type: "Timeout Error"},
	}
}

type FreeCapValidationError struct {
	*FreeCapError
}

func NewFreeCapValidationError(message string) *FreeCapValidationError {
	return &FreeCapValidationError{
		FreeCapError: &FreeCapError{Message: message, Type: "Validation Error"},
	}
}

// Logger interface
type Logger interface {
	Debug(message string, args ...interface{})
	Info(message string, args ...interface{})
	Warning(message string, args ...interface{})
	Error(message string, args ...interface{})
}

// ConsoleLogger implements Logger interface
type ConsoleLogger struct {
	logger *log.Logger
}

func NewConsoleLogger() *ConsoleLogger {
	return &ConsoleLogger{
		logger: log.New(os.Stdout, "freecap_client: ", log.LstdFlags),
	}
}

func (c *ConsoleLogger) Debug(message string, args ...interface{}) {
	c.logger.Printf("[DEBUG] "+message, args...)
}

func (c *ConsoleLogger) Info(message string, args ...interface{}) {
	c.logger.Printf("[INFO] "+message, args...)
}

func (c *ConsoleLogger) Warning(message string, args ...interface{}) {
	c.logger.Printf("[WARNING] "+message, args...)
}

func (c *ConsoleLogger) Error(message string, args ...interface{}) {
	c.logger.Printf("[ERROR] "+message, args...)
}

// NullLogger discards all log messages
type NullLogger struct{}

func (n *NullLogger) Debug(message string, args ...interface{})   {}
func (n *NullLogger) Info(message string, args ...interface{})    {}
func (n *NullLogger) Warning(message string, args ...interface{}) {}
func (n *NullLogger) Error(message string, args ...interface{})   {}

// ClientConfig holds client configuration options
type ClientConfig struct {
	APIURL               string
	RequestTimeout       time.Duration
	MaxRetries           int
	RetryDelay           time.Duration
	DefaultTaskTimeout   time.Duration
	DefaultCheckInterval time.Duration
	UserAgent            string
}

// NewClientConfig creates a default client configuration
func NewClientConfig() *ClientConfig {
	return &ClientConfig{
		APIURL:               "https://freecap.app",
		RequestTimeout:       30 * time.Second,
		MaxRetries:           3,
		RetryDelay:           1 * time.Second,
		DefaultTaskTimeout:   120 * time.Second,
		DefaultCheckInterval: 3 * time.Second,
		UserAgent:            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36",
	}
}

// FreeCapClient is the main client for FreeCap API
type FreeCapClient struct {
	apiKey string
	config *ClientConfig
	logger Logger
	client *http.Client
	closed bool
}

// NewFreeCapClient creates a new FreeCap client
func NewFreeCapClient(apiKey string, config *ClientConfig, logger Logger) (*FreeCapClient, error) {
	if strings.TrimSpace(apiKey) == "" {
		return nil, NewFreeCapValidationError("API key cannot be empty")
	}

	if config == nil {
		config = NewClientConfig()
	}

	if logger == nil {
		logger = NewConsoleLogger()
	}

	if !strings.HasPrefix(config.APIURL, "http://") && !strings.HasPrefix(config.APIURL, "https://") {
		return nil, NewFreeCapValidationError("API URL must start with http:// or https://")
	}

	return &FreeCapClient{
		apiKey: strings.TrimSpace(apiKey),
		config: config,
		logger: logger,
		client: &http.Client{
			Timeout: config.RequestTimeout,
		},
		closed: false,
	}, nil
}

// validateTask validates task configuration for specific captcha type
func (c *FreeCapClient) validateTask(task *CaptchaTask, captchaType CaptchaType) error {
	switch captchaType {
	case HCaptcha:
		if task.Sitekey == "" {
			return NewFreeCapValidationError("sitekey is required for hCaptcha")
		}
		if task.Siteurl == "" {
			return NewFreeCapValidationError("siteurl is required for hCaptcha")
		}
		if task.GroqAPIKey == "" {
			return NewFreeCapValidationError("groq_api_key is required for hCaptcha")
		}
		if task.RqData == "" {
			return NewFreeCapValidationError("rqdata cannot be blank for Discord hCaptcha")
		}
	case CaptchaFox:
		if task.Sitekey == "" {
			return NewFreeCapValidationError("sitekey is required for CaptchaFox")
		}
		if task.Siteurl == "" {
			return NewFreeCapValidationError("siteurl is required for CaptchaFox")
		}
	case DiscordID:
		if task.Sitekey == "" {
			return NewFreeCapValidationError("sitekey is required for Discord ID")
		}
		if task.Siteurl == "" {
			return NewFreeCapValidationError("siteurl is required for Discord ID")
		}
	case Geetest:
		if task.Challenge == "" {
			return NewFreeCapValidationError("challenge is required for Geetest")
		}
	case FunCaptcha:
		if task.Preset == "" {
			return NewFreeCapValidationError("preset is required for FunCaptcha")
		}
		if task.ChromeVersion != "136" && task.ChromeVersion != "137" {
			return NewFreeCapValidationError("chrome_version must be 136 or 137 for FunCaptcha")
		}
	}
	return nil
}

// buildPayload builds API payload for specific captcha type
func (c *FreeCapClient) buildPayload(task *CaptchaTask, captchaType CaptchaType) (map[string]interface{}, error) {
	if err := c.validateTask(task, captchaType); err != nil {
		return nil, err
	}

	payloadData := make(map[string]interface{})

	switch captchaType {
	case HCaptcha:
		payloadData["websiteURL"] = task.Siteurl
		payloadData["websiteKey"] = task.Sitekey
		payloadData["rqData"] = task.RqData
		payloadData["groqApiKey"] = task.GroqAPIKey
	case CaptchaFox:
		payloadData["websiteURL"] = task.Siteurl
		payloadData["websiteKey"] = task.Sitekey
	case Geetest:
		payloadData["Challenge"] = task.Challenge
		if task.RiskType != "" {
			payloadData["RiskType"] = string(task.RiskType)
		} else {
			payloadData["RiskType"] = string(Slide)
		}
	case DiscordID:
		payloadData["websiteURL"] = task.Siteurl
		payloadData["websiteKey"] = task.Sitekey
	case FunCaptcha:
		payloadData["preset"] = string(task.Preset)
		payloadData["chrome_version"] = task.ChromeVersion
		payloadData["blob"] = task.Blob
	case AuroNetwork:
		// No specific fields required
	}

	if task.Proxy != "" {
		payloadData["proxy"] = task.Proxy
	}

	return map[string]interface{}{
		"captchaType": string(captchaType),
		"payload":     payloadData,
	}, nil
}

// makeRequest makes HTTP request with retries
func (c *FreeCapClient) makeRequest(ctx context.Context, method, endpoint string, data map[string]interface{}) (map[string]interface{}, error) {
	if c.closed {
		return nil, errors.New("client has been closed")
	}

	url := strings.TrimRight(c.config.APIURL, "/") + "/" + strings.TrimLeft(endpoint, "/")
	var lastErr error

	for attempt := 0; attempt <= c.config.MaxRetries; attempt++ {
		c.logger.Debug("Making %s request to %s (attempt %d)", method, url, attempt+1)

		var reqBody io.Reader
		if data != nil {
			jsonData, err := json.Marshal(data)
			if err != nil {
				return nil, fmt.Errorf("failed to marshal request data: %w", err)
			}
			reqBody = bytes.NewBuffer(jsonData)
		}

		req, err := http.NewRequestWithContext(ctx, method, url, reqBody)
		if err != nil {
			return nil, fmt.Errorf("failed to create request: %w", err)
		}

		req.Header.Set("X-API-Key", c.apiKey)
		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("User-Agent", c.config.UserAgent)
		req.Header.Set("Accept", "application/json")

		resp, err := c.client.Do(req)
		if err != nil {
			errorMsg := fmt.Sprintf("Network error: %s", err.Error())
			c.logger.Warning("%s (attempt %d)", errorMsg, attempt+1)
			lastErr = NewFreeCapAPIError(errorMsg, 0, nil)

			if attempt < c.config.MaxRetries {
				delay := c.config.RetryDelay * time.Duration(1<<attempt)
				time.Sleep(delay)
				continue
			}
			break
		}

		body, err := io.ReadAll(resp.Body)
		resp.Body.Close()
		if err != nil {
			return nil, fmt.Errorf("failed to read response body: %w", err)
		}

		var responseData map[string]interface{}
		if len(body) > 0 {
			if err := json.Unmarshal(body, &responseData); err != nil {
				responseData = map[string]interface{}{"raw_response": string(body)}
			}
		}

		if resp.StatusCode == 200 {
			return responseData, nil
		}

		switch resp.StatusCode {
		case 401:
			return nil, NewFreeCapAPIError("Invalid API key", resp.StatusCode, responseData)
		case 429:
			return nil, NewFreeCapAPIError("Rate limit exceeded", resp.StatusCode, responseData)
		default:
			if resp.StatusCode >= 500 {
				errorMsg := fmt.Sprintf("Server error %d: %s", resp.StatusCode, string(body))
				c.logger.Warning("%s (attempt %d)", errorMsg, attempt+1)
				lastErr = NewFreeCapAPIError(errorMsg, resp.StatusCode, responseData)

				if attempt < c.config.MaxRetries {
					delay := c.config.RetryDelay * time.Duration(1<<attempt)
					time.Sleep(delay)
					continue
				}
			} else {
				return nil, NewFreeCapAPIError(
					fmt.Sprintf("HTTP error %d: %s", resp.StatusCode, string(body)),
					resp.StatusCode,
					responseData,
				)
			}
		}
	}

	if lastErr != nil {
		return nil, lastErr
	}
	return nil, NewFreeCapAPIError("Max retries exceeded", 0, nil)
}

// CreateTask creates a captcha solving task
func (c *FreeCapClient) CreateTask(ctx context.Context, task *CaptchaTask, captchaType CaptchaType) (string, error) {
	payload, err := c.buildPayload(task, captchaType)
	if err != nil {
		return "", err
	}

	c.logger.Info("Creating %s task for %s", string(captchaType), task.Siteurl)
	c.logger.Debug("Task payload: %+v", payload)

	response, err := c.makeRequest(ctx, "POST", "/CreateTask", payload)
	if err != nil {
		return "", err
	}

	status, ok := response["status"]
	if !ok || status != true {
		errorMsg := "Unknown error creating task"
		if errVal, exists := response["error"]; exists {
			if errStr, ok := errVal.(string); ok {
				errorMsg = errStr
			}
		}
		return "", NewFreeCapAPIError(fmt.Sprintf("Failed to create task: %s", errorMsg), 0, response)
	}

	taskID, ok := response["taskId"]
	if !ok {
		return "", NewFreeCapAPIError("No task ID in response", 0, response)
	}

	taskIDStr, ok := taskID.(string)
	if !ok {
		return "", NewFreeCapAPIError("Invalid task ID format", 0, response)
	}

	c.logger.Info("Task created successfully: %s", taskIDStr)
	return taskIDStr, nil
}

// GetTaskResult gets task result by ID
func (c *FreeCapClient) GetTaskResult(ctx context.Context, taskID string) (map[string]interface{}, error) {
	if strings.TrimSpace(taskID) == "" {
		return nil, NewFreeCapValidationError("Task ID cannot be empty")
	}

	payload := map[string]interface{}{
		"taskId": strings.TrimSpace(taskID),
	}

	c.logger.Debug("Checking task status: %s", taskID)

	return c.makeRequest(ctx, "POST", "/GetTask", payload)
}

// SolveCaptcha solves a captcha and returns the solution
func (c *FreeCapClient) SolveCaptcha(ctx context.Context, task *CaptchaTask, captchaType CaptchaType, timeout, checkInterval time.Duration) (string, error) {
	if timeout <= 0 {
		timeout = c.config.DefaultTaskTimeout
	}
	if checkInterval <= 0 {
		checkInterval = c.config.DefaultCheckInterval
	}

	if timeout <= 0 {
		return "", NewFreeCapValidationError("Timeout must be positive")
	}
	if checkInterval <= 0 {
		return "", NewFreeCapValidationError("Check interval must be positive")
	}

	taskID, err := c.CreateTask(ctx, task, captchaType)
	if err != nil {
		return "", err
	}

	c.logger.Info("Waiting for task %s to complete (timeout: %v)", taskID, timeout)

	timeoutCtx, cancel := context.WithTimeout(ctx, timeout)
	defer cancel()

	ticker := time.NewTicker(checkInterval)
	defer ticker.Stop()

	for {
		select {
		case <-timeoutCtx.Done():
			return "", NewFreeCapTimeoutError(fmt.Sprintf("Task %s timed out after %v", taskID, timeout))
		case <-ticker.C:
			result, err := c.GetTaskResult(ctx, taskID)
			if err != nil {
				c.logger.Warning("Error checking task %s: %v", taskID, err)
				continue
			}

			statusVal, ok := result["status"]
			if !ok {
				c.logger.Warning("No status in response for task %s", taskID)
				continue
			}

			status, ok := statusVal.(string)
			if !ok {
				c.logger.Warning("Invalid status format for task %s", taskID)
				continue
			}

			status = strings.ToLower(status)
			c.logger.Debug("Task %s status: %s", taskID, status)

			switch TaskStatus(status) {
			case Solved:
				solution, ok := result["solution"]
				if !ok {
					return "", NewFreeCapAPIError(
						fmt.Sprintf("Task %s marked as solved but no solution provided", taskID),
						0, result,
					)
				}

				solutionStr, ok := solution.(string)
				if !ok {
					return "", NewFreeCapAPIError(
						fmt.Sprintf("Task %s solution is not a string", taskID),
						0, result,
					)
				}

				c.logger.Info("Task %s solved successfully", taskID)
				return solutionStr, nil

			case Error, Failed:
				var errorMessage string
				if errVal, exists := result["error"]; exists {
					if errStr, ok := errVal.(string); ok {
						errorMessage = errStr
					}
				}
				if errorMessage == "" {
					if errVal, exists := result["Error"]; exists {
						if errStr, ok := errVal.(string); ok {
							errorMessage = errStr
						}
					}
				}
				if errorMessage == "" {
					errorMessage = "Unknown error"
				}

				return "", NewFreeCapAPIError(
					fmt.Sprintf("Task %s failed: %s", taskID, errorMessage),
					0, result,
				)

			case Processing, Pending:
				remaining := timeout - time.Since(timeoutCtx.Value("start_time").(time.Time))
				c.logger.Debug("Task %s still %s, %v remaining", taskID, status, remaining)

			default:
				c.logger.Warning("Unknown task status for %s: %s", taskID, status)
			}
		}
	}
}

// Close closes the client and cleanup resources
func (c *FreeCapClient) Close() {
	if c.closed {
		return
	}
	c.closed = true
	c.logger.Debug("Client closed")
}

// Convenience functions

// SolveHCaptcha solves hCaptcha with provided parameters
func SolveHCaptcha(ctx context.Context, apiKey, sitekey, siteurl, rqdata, groqAPIKey, proxy string, timeout time.Duration) (string, error) {
	client, err := NewFreeCapClient(apiKey, nil, nil)
	if err != nil {
		return "", err
	}
	defer client.Close()

	task := &CaptchaTask{
		Sitekey:    sitekey,
		Siteurl:    siteurl,
		RqData:     rqdata,
		GroqAPIKey: groqAPIKey,
		Proxy:      proxy,
	}

	if timeout <= 0 {
		timeout = 120 * time.Second
	}

	return client.SolveCaptcha(ctx, task, HCaptcha, timeout, 0)
}

// SolveFunCaptcha solves FunCaptcha with provided parameters
func SolveFunCaptcha(ctx context.Context, apiKey string, preset FunCaptchaPreset, chromeVersion, blob, proxy string, timeout time.Duration) (string, error) {
	client, err := NewFreeCapClient(apiKey, nil, nil)
	if err != nil {
		return "", err
	}
	defer client.Close()

	if chromeVersion == "" {
		chromeVersion = "137"
	}
	if blob == "" {
		blob = "undefined"
	}

	task := &CaptchaTask{
		Preset:        preset,
		ChromeVersion: chromeVersion,
		Blob:          blob,
		Proxy:         proxy,
	}

	if timeout <= 0 {
		timeout = 120 * time.Second
	}

	return client.SolveCaptcha(ctx, task, FunCaptcha, timeout, 0)
}

// Example usage
func main() {
	ctx := context.Background()

	client, err := NewFreeCapClient("your-api-key", nil, nil)
	if err != nil {
		log.Fatalf("Failed to create client: %v", err)
	}
	defer client.Close()

	task := &CaptchaTask{
		Sitekey:    "a9b5fb07-92ff-493f-86fe-352a2803b3df",
		Siteurl:    "discord.com",
		RqData:     "your-rq-data-here",
		GroqAPIKey: "your-groq-api-key",
		Proxy:      "http://user:pass@host:port",
	}

	solution, err := client.SolveCaptcha(
		ctx,
		task,
		HCaptcha,
		180*time.Second,
		3*time.Second,
	)

	if err != nil {
		switch e := err.(type) {
		case *FreeCapValidationError:
			log.Printf("❌ Validation error: %v", e)
		case *FreeCapTimeoutError:
			log.Printf("⏰ Timeout error: %v", e)
		case *FreeCapAPIError:
			log.Printf("🌐 API error: %v", e)
			if e.StatusCode != 0 {
				log.Printf("   Status code: %d", e.StatusCode)
			}
			if e.ResponseData != nil {
				log.Printf("   Response: %+v", e.ResponseData)
			}
		default:
			log.Printf("💥 Unexpected error: %v", e)
		}
		return
	}

	log.Printf("✅ hCaptcha solved: %s", solution)
}
