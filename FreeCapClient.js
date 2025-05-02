/**
 * Represents a captcha task to be solved
 */
class CaptchaTask {
  /**
   * @param {string} sitekey - The captcha sitekey
   * @param {string} siteurl - The website URL where captcha appears
   * @param {string} proxy - Proxy to use (optional)
   * @param {string|null} rqdata - Additional request data for hCaptcha (optional)
   */
  constructor(sitekey, siteurl, proxy, rqdata = null) {
    this.sitekey = sitekey;
    this.siteurl = siteurl;
    this.proxy = proxy;
    this.rqdata = rqdata;
  }
}

/**
 * Logger interface
 */
class ILogger {
  /**
   * @param {string} message - Message to log
   */
  info(message) {}
}

/**
 * Console implementation of the logger
 */
class ConsoleLogger extends ILogger {
  /**
   * @param {string} message - Message to log
   */
  info(message) {
    console.log(`[INFO] ${message}`);
  }
}

/**
 * Client for interacting with FreeCap API
 */
class FreeCapClient {
  /**
   * @param {string} apiKey - Your FreeCap API key
   * @param {string} apiUrl - FreeCap API URL (default: https://freecap.app)
   * @param {ILogger|null} logger - Logger instance (optional)
   */
  constructor(apiKey, apiUrl = "https://freecap.app", logger = null) {
    this._apiUrl = apiUrl.endsWith('/') ? apiUrl.slice(0, -1) : apiUrl;
    this._logger = logger || new ConsoleLogger();
    this._headers = {
      "X-API-Key": apiKey,
      "Content-Type": "application/json"
    };
  }

  /**
   * Creates a new captcha solving task
   * @param {CaptchaTask} task - The captcha task to solve
   * @param {string} captchaType - Type of captcha (default: "hcaptcha")
   * @returns {Promise<object>} - Task creation result
   */
  async createTaskAsync(task, captchaType = "hcaptcha") {
    const taskData = {
      captchaType: captchaType,
      payload: {
        websiteURL: task.siteurl,
        websiteKey: task.sitekey
      }
    };
    
    if (task.proxy) {
      taskData.payload.proxy = task.proxy;
    }
    
    if (task.rqdata && captchaType === "hcaptcha") {
      taskData.payload.rqdata = task.rqdata;
    }
    
    this._logger.info(`Creating ${captchaType} task for site: ${task.siteurl}`);
    
    const response = await fetch(`${this._apiUrl}/CreateTask`, {
      method: 'POST',
      headers: this._headers,
      body: JSON.stringify(taskData)
    });
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP error ${response.status}: ${errorText}`);
    }
    
    const result = await response.json();
    
    if (!result.status || !('taskId' in result)) {
      throw new Error(`Error creating task: ${JSON.stringify(result)}`);
    }
    
    return result;
  }

  /**
   * Gets the result of a captcha solving task
   * @param {string} taskId - The ID of the task to check
   * @returns {Promise<object>} - Task result
   */
  async getResultAsync(taskId) {
    const requestData = { taskId: taskId };
    
    const response = await fetch(`${this._apiUrl}/GetTask`, {
      method: 'POST',
      headers: this._headers,
      body: JSON.stringify(requestData)
    });
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP error ${response.status}: ${errorText}`);
    }
    
    return await response.json();
  }

  /**
   * Solves a captcha by creating a task and polling for the result
   * @param {CaptchaTask} task - The captcha task to solve
   * @param {string} captchaType - Type of captcha (default: "hcaptcha")
   * @param {number} timeout - Maximum time to wait in seconds (default: 120)
   * @param {number} checkInterval - Interval between result checks in seconds (default: 3)
   * @returns {Promise<string>} - The captcha solution
   */
  async solveCaptchaAsync(task, captchaType = "hcaptcha", timeout = 120, checkInterval = 3) {
    const taskResult = await this.createTaskAsync(task, captchaType);
    const taskId = taskResult.taskId || taskResult.task_id;
    
    if (!taskId) {
      throw new Error(`Invalid task result: ${JSON.stringify(taskResult)}`);
    }
    
    const startTime = Date.now();
    while (true) {
      if ((Date.now() - startTime) / 1000 > timeout) {
        throw new Error(`Task ${taskId} timed out after ${timeout} seconds`);
      }
      
      const result = await this.getResultAsync(taskId);
      
      if (result.status === "Solved") {
        this._logger.info(`Task ${taskId} solved successfully`);
        return result.solution;
      } else if (result.status === "Error") {
        const errorMessage = result.Error || "Unknown error";
        throw new Error(`Task ${taskId} failed: ${errorMessage}`);
      }
      
      await new Promise(resolve => setTimeout(resolve, checkInterval * 1000));
    }
  }
}

if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    CaptchaTask,
    ILogger,
    ConsoleLogger,
    FreeCapClient
  };
}
