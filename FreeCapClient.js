const axios = require('axios');

/**
 * @typedef {Object} CaptchaTask
 * @property {string} sitekey - The captcha site key
 * @property {string} siteurl - The website URL where captcha is located
 * @property {string} proxy - Proxy to use for solving the captcha
 * @property {string} [rqdata] - Optional rqdata for hCaptcha
 */

/**
 * Client for FreeCap API to solve various captchas
 */
class FreeCapClient {
  /**
   * Create a new FreeCap client
   * @param {string} apiKey - Your FreeCap API key
   * @param {string} [apiUrl='https://freecap.app'] - FreeCap API URL
   */
  constructor(apiKey, apiUrl = 'https://freecap.app') {
    this.apiKey = apiKey;
    this.apiUrl = apiUrl.endsWith('/') ? apiUrl.slice(0, -1) : apiUrl;
    this.logger = console; // Using console as logger
  }

  /**
   * Create a new captcha solving task
   * @param {CaptchaTask} task - The captcha task details
   * @param {'hcaptcha'|'captchafox'} [captchaType='hcaptcha'] - Type of captcha to solve
   * @returns {Promise<Object>} - Task creation response
   */
  async createTask(task, captchaType = 'hcaptcha') {
    const taskData = {
      freecap_key: this.apiKey,
      captcha_type: captchaType,
      payload: {
        sitekey: task.sitekey,
        siteurl: task.siteurl
      }
    };

    if (task.proxy) {
      taskData.payload.proxy = task.proxy;
    }

    if (task.rqdata && captchaType === 'hcaptcha') {
      taskData.payload.rqdata = task.rqdata;
    }

    this.logger.info(`Creating ${captchaType} task for site: ${task.siteurl}`);
    
    try {
      const response = await axios.post(`${this.apiUrl}/create_task`, taskData);
      const result = response.data;
      
      if (!result.success || !result.task_id) {
        throw new Error(`Error creating task: ${JSON.stringify(result)}`);
      }
      
      return result;
    } catch (error) {
      if (error.response) {
        throw new Error(`API error: ${error.response.status} - ${JSON.stringify(error.response.data)}`);
      }
      throw error;
    }
  }

  /**
   * Get the result of a captcha solving task
   * @param {string} taskId - The task ID to check
   * @returns {Promise<Object>} - Task result
   */
  async getResult(taskId) {
    try {
      const response = await axios.post(`${this.apiUrl}/get_task`, {
        freecap_key: this.apiKey,
        task_id: taskId
      });
      
      return response.data;
    } catch (error) {
      if (error.response) {
        throw new Error(`API error: ${error.response.status} - ${JSON.stringify(error.response.data)}`);
      }
      throw error;
    }
  }

  /**
   * Solve a captcha task from start to finish
   * @param {CaptchaTask} task - The captcha task details
   * @param {'hcaptcha'|'captchafox'} [captchaType='hcaptcha'] - Type of captcha to solve
   * @param {number} [timeout=120] - Maximum time in seconds to wait for solution
   * @param {number} [checkInterval=3] - Time in seconds between result checks
   * @returns {Promise<string|Object>} - Captcha solution token or object (for Geetest)
   */
  async solveCaptcha(task, captchaType = 'hcaptcha', timeout = 120, checkInterval = 3) {
    const taskResult = await this.createTask(task, captchaType);
    const taskId = taskResult.task_id;
    
    const startTime = Date.now();
    
    while (true) {
      if ((Date.now() - startTime) / 1000 > timeout) {
        throw new Error(`Task ${taskId} timed out after ${timeout} seconds`);
      }
      
      const result = await this.getResult(taskId);
      
      if (result.status === 'solved') {
        this.logger.info(`Task ${taskId} solved successfully`);
        return result.captcha_token;
      } else if (result.status === 'error') {
        throw new Error(`Task ${taskId} failed: ${result.error || 'Unknown error'}`);
      }
      
      // Wait for check interval
      await new Promise(resolve => setTimeout(resolve, checkInterval * 1000));
    }
  }
}


module.exports = {
  FreeCapClient
};