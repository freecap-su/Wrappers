package freecap

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"strings"
	"time"
)

type CaptchaTask struct {
	Sitekey string
	Siteurl string
	Proxy   string
	Rqdata  string
}

type FreeCapClient struct {
	ApiKey  string
	ApiUrl  string
	Logger  Logger
}

type Logger interface {
	Info(msg string, args ...interface{})
	Error(msg string, args ...interface{})
}

type DefaultLogger struct{}

func (l *DefaultLogger) Info(msg string, args ...interface{}) {
	fmt.Printf("[INFO] "+msg+"\n", args...)
}

func (l *DefaultLogger) Error(msg string, args ...interface{}) {
	fmt.Printf("[ERROR] "+msg+"\n", args...)
}

func NewFreeCapClient(apiKey string, apiUrl string) *FreeCapClient {
	if apiUrl == "" {
		apiUrl = "https://freecap.app"
	}
	
	apiUrl = strings.TrimSuffix(apiUrl, "/")
	
	return &FreeCapClient{
		ApiKey:  apiKey,
		ApiUrl:  apiUrl,
		Logger:  &DefaultLogger{},
	}
}

func (c *FreeCapClient) SetLogger(logger Logger) {
	c.Logger = logger
}

type CreateTaskRequest struct {
	FreecapKey  string      `json:"freecap_key"`
	CaptchaType string      `json:"captcha_type"`
	Payload     TaskPayload `json:"payload"`
}

type TaskPayload struct {
	Sitekey string `json:"sitekey"`
	Siteurl string `json:"siteurl"`
	Proxy   string `json:"proxy,omitempty"`
	Rqdata  string `json:"rqdata,omitempty"`
}

type CreateTaskResponse struct {
	Success bool   `json:"success"`
	TaskID  string `json:"task_id"`
	Error   string `json:"error"`
}

type GetTaskRequest struct {
	FreecapKey string `json:"freecap_key"`
	TaskID     string `json:"task_id"`
}

type GetTaskResponse struct {
	Status       string `json:"status"`
	CaptchaToken string `json:"captcha_token"`
	Error        string `json:"error"`
}

func (c *FreeCapClient) CreateTask(task CaptchaTask, captchaType string) (*CreateTaskResponse, error) {
	if captchaType == "" {
		captchaType = "hcaptcha"
	}
	
	taskData := CreateTaskRequest{
		FreecapKey:  c.ApiKey,
		CaptchaType: captchaType,
		Payload: TaskPayload{
			Sitekey: task.Sitekey,
			Siteurl: task.Siteurl,
		},
	}
	
	if task.Proxy != "" {
		taskData.Payload.Proxy = task.Proxy
	}
	
	if task.Rqdata != "" && captchaType == "hcaptcha" {
		taskData.Payload.Rqdata = task.Rqdata
	}
	
	c.Logger.Info("Creating %s task for site: %s", captchaType, task.Siteurl)
	
	jsonData, err := json.Marshal(taskData)
	if err != nil {
		return nil, err
	}
	
	resp, err := http.Post(
		fmt.Sprintf("%s/create_task", c.ApiUrl),
		"application/json",
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("API request failed with status: %d", resp.StatusCode)
	}
	
	var result CreateTaskResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	
	if !result.Success || result.TaskID == "" {
		return nil, fmt.Errorf("error creating task: %s", result.Error)
	}
	
	return &result, nil
}

func (c *FreeCapClient) GetResult(taskID string) (*GetTaskResponse, error) {
	requestData := GetTaskRequest{
		FreecapKey: c.ApiKey,
		TaskID:     taskID,
	}
	
	jsonData, err := json.Marshal(requestData)
	if err != nil {
		return nil, err
	}
	
	resp, err := http.Post(
		fmt.Sprintf("%s/get_task", c.ApiUrl),
		"application/json",
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("API request failed with status: %d", resp.StatusCode)
	}
	
	var result GetTaskResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	
	return &result, nil
}

func (c *FreeCapClient) SolveCaptcha(task CaptchaTask, captchaType string, timeout int, checkInterval int) (string, error) {
	if captchaType == "" {
		captchaType = "hcaptcha"
	}
	
	if timeout <= 0 {
		timeout = 120
	}
	
	if checkInterval <= 0 {
		checkInterval = 3
	}
	
	taskResult, err := c.CreateTask(task, captchaType)
	if err != nil {
		return "", err
	}
	
	taskID := taskResult.TaskID
	startTime := time.Now()
	
	for {
		if time.Since(startTime).Seconds() > float64(timeout) {
			return "", fmt.Errorf("task %s timed out after %d seconds", taskID, timeout)
		}
		
		result, err := c.GetResult(taskID)
		if err != nil {
			return "", err
		}
		
		if result.Status == "solved" {
			c.Logger.Info("Task %s solved successfully", taskID)
			return result.CaptchaToken, nil
		} else if result.Status == "error" {
			errMsg := result.Error
			if errMsg == "" {
				errMsg = "Unknown error"
			}
			return "", errors.New(fmt.Sprintf("Task %s failed: %s", taskID, errMsg))
		}
		
		time.Sleep(time.Duration(checkInterval) * time.Second)
	}
}