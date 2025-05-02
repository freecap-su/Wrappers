package freecap

import (
	"bytes"
	"encoding/json"
	"errors"
	"fmt"
	"io"
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

type ILogger interface {
	Info(message string)
}

type ConsoleLogger struct{}

func (l *ConsoleLogger) Info(message string) {
	fmt.Printf("[INFO] %s\n", message)
}

type TaskResult struct {
	Status string `json:"status"`
	TaskID string `json:"taskId"`
}

type SolutionResult struct {
	Status   string `json:"status"`
	Solution string `json:"solution"`
	Error    string `json:"Error"`
}

type FreeCapClient struct {
	apiURL  string
	apiKey  string
	logger  ILogger
	client  *http.Client
}

func NewFreeCapClient(apiKey string, apiURL string, logger ILogger) *FreeCapClient {
	if apiURL == "" {
		apiURL = "https://freecap.app"
	}
	
	apiURL = strings.TrimRight(apiURL, "/")
	
	if logger == nil {
		logger = &ConsoleLogger{}
	}
	
	return &FreeCapClient{
		apiURL:  apiURL,
		apiKey:  apiKey,
		logger:  logger,
		client:  &http.Client{},
	}
}

func (fc *FreeCapClient) CreateTask(task CaptchaTask, captchaType string) (*TaskResult, error) {
	if captchaType == "" {
		captchaType = "hcaptcha"
	}
	
	taskData := map[string]interface{}{
		"captchaType": captchaType,
		"payload": map[string]interface{}{
			"websiteURL": task.Siteurl,
			"websiteKey": task.Sitekey,
		},
	}
	
	payload := taskData["payload"].(map[string]interface{})
	if task.Proxy != "" {
		payload["proxy"] = task.Proxy
	}
	
	if task.Rqdata != "" && captchaType == "hcaptcha" {
		payload["rqdata"] = task.Rqdata
	}
	
	fc.logger.Info(fmt.Sprintf("Creating %s task for site: %s", captchaType, task.Siteurl))
	
	jsonData, err := json.Marshal(taskData)
	if err != nil {
		return nil, err
	}
	
	req, err := http.NewRequest("POST", fc.apiURL+"/CreateTask", bytes.NewBuffer(jsonData))
	if err != nil {
		return nil, err
	}
	
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-API-Key", fc.apiKey)
	
	resp, err := fc.client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}
	
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("HTTP error %d: %s", resp.StatusCode, string(body))
	}
	
	var result TaskResult
	if err := json.Unmarshal(body, &result); err != nil {
		return nil, err
	}
	
	if result.Status == "" || result.TaskID == "" {
		return nil, fmt.Errorf("Error creating task: %s", string(body))
	}
	
	return &result, nil
}

func (fc *FreeCapClient) GetResult(taskID string) (*SolutionResult, error) {
	requestData := map[string]string{
		"taskId": taskID,
	}
	
	jsonData, err := json.Marshal(requestData)
	if err != nil {
		return nil, err
	}
	
	req, err := http.NewRequest("POST", fc.apiURL+"/GetTask", bytes.NewBuffer(jsonData))
	if err != nil {
		return nil, err
	}
	
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-API-Key", fc.apiKey)
	
	resp, err := fc.client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}
	
	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("HTTP error %d: %s", resp.StatusCode, string(body))
	}
	
	var result SolutionResult
	if err := json.Unmarshal(body, &result); err != nil {
		return nil, err
	}
	
	return &result, nil
}

func (fc *FreeCapClient) SolveCaptcha(task CaptchaTask, captchaType string, timeout int, checkInterval int) (string, error) {
	if captchaType == "" {
		captchaType = "hcaptcha"
	}
	
	if timeout <= 0 {
		timeout = 120
	}
	
	if checkInterval <= 0 {
		checkInterval = 3
	}
	
	taskResult, err := fc.CreateTask(task, captchaType)
	if err != nil {
		return "", err
	}
	
	taskID := taskResult.TaskID
	if taskID == "" {
		return "", fmt.Errorf("Invalid task result: %v", taskResult)
	}
	
	startTime := time.Now()
	for {
		if time.Since(startTime).Seconds() > float64(timeout) {
			return "", fmt.Errorf("Task %s timed out after %d seconds", taskID, timeout)
		}
		
		result, err := fc.GetResult(taskID)
		if err != nil {
			return "", err
		}
		
		if result.Status == "Solved" {
			fc.logger.Info(fmt.Sprintf("Task %s solved successfully", taskID))
			return result.Solution, nil
		} else if result.Status == "Error" {
			errorMessage := "Unknown error"
			if result.Error != "" {
				errorMessage = result.Error
			}
			return "", fmt.Errorf("Task %s failed: %s", taskID, errorMessage)
		}
		
		time.Sleep(time.Duration(checkInterval) * time.Second)
	}
}
