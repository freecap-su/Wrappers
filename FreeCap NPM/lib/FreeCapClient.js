/**
 * FreeCap API Client - Professional JavaScript Implementation
 * 
 * A robust, production-ready async client for the FreeCap captcha solving service.
 * Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.
 * 
 * Author: FreeCap Client
 * Version: 1.0.0
 * License: GPLv3
 */

const fetch = require('node-fetch');

/**
 * Supported captcha types
 */
const CaptchaType = Object.freeze({
    HCAPTCHA: "hcaptcha",
    CAPTCHAFOX: "captchafox",
    GEETEST: "geetest",
    DISCORD_ID: "discordid",
    FUNCAPTCHA: "funcaptcha",
    AURO_NETWORK: "auronetwork"
});

/**
 * Task status values
 */
const TaskStatus = Object.freeze({
    PENDING: "pending",
    PROCESSING: "processing",
    SOLVED: "solved",
    ERROR: "error",
    FAILED: "failed"
});

/**
 * Geetest risk types
 */
const RiskType = Object.freeze({
    SLIDE: "slide",
    GOBANG: "gobang",
    ICON: "icon",
    AI: "ai"
});

/**
 * FunCaptcha presets
 */
const FunCaptchaPreset = Object.freeze({
    ROBLOX_LOGIN: "roblox_login",
    ROBLOX_FOLLOW: "roblox_follow",
    ROBLOX_GROUP: "roblox_group",
    DROPBOX_LOGIN: "dropbox_login"
});

/**
 * Captcha task configuration class
 */
class CaptchaTask {
    /**
     * Create a captcha task
     * @param {Object} options - Task configuration options
     * @param {string} [options.sitekey] - Site key for captcha
     * @param {string} [options.siteurl] - Website URL
     * @param {string} [options.proxy] - Proxy configuration
     * @param {string} [options.rqdata] - rqData for hCaptcha
     * @param {string} [options.groq_api_key] - Groq API key for hCaptcha
     * @param {string} [options.challenge] - Challenge for Geetest
     * @param {string} [options.risk_type] - Risk type for Geetest
     * @param {string} [options.preset] - Preset for FunCaptcha
     * @param {string} [options.chrome_version="137"] - Chrome version for FunCaptcha
     * @param {string} [options.blob="undefined"] - Blob for FunCaptcha
     */
    constructor(options = {}) {
        this.sitekey = options.sitekey || null;
        this.siteurl = options.siteurl || null;
        this.proxy = options.proxy || null;
        this.rqdata = options.rqdata || null;
        this.groq_api_key = options.groq_api_key || null;
        this.challenge = options.challenge || null;
        this.risk_type = options.risk_type || null;
        this.preset = options.preset || null;
        this.chrome_version = options.chrome_version || "137";
        this.blob = options.blob || "undefined";
    }
}

/**
 * Base exception for FreeCap client errors
 */
class FreeCapException extends Error {
    constructor(message) {
        super(message);
        this.name = 'FreeCapException';
    }
}

/**
 * Exception raised for API-related errors
 */
class FreeCapAPIException extends FreeCapException {
    constructor(message, statusCode = null, responseData = null) {
        super(message);
        this.name = 'FreeCapAPIException';
        this.statusCode = statusCode;
        this.responseData = responseData;
    }
}

/**
 * Exception raised when a task times out
 */
class FreeCapTimeoutException extends FreeCapException {
    constructor(message) {
        super(message);
        this.name = 'FreeCapTimeoutException';
    }
}

/**
 * Exception raised for validation errors
 */
class FreeCapValidationException extends FreeCapException {
    constructor(message) {
        super(message);
        this.name = 'FreeCapValidationException';
    }
}

/**
 * Abstract logger interface
 */
class ILogger {
    debug(message, ...args) {
        throw new Error('Abstract method must be implemented');
    }
    
    info(message, ...args) {
        throw new Error('Abstract method must be implemented');
    }
    
    warning(message, ...args) {
        throw new Error('Abstract method must be implemented');
    }
    
    error(message, ...args) {
        throw new Error('Abstract method must be implemented');
    }
}

/**
 * Simple console logger implementation
 */
class ConsoleLogger extends ILogger {
    constructor(level = 'info') {
        super();
        this.level = level;
        this.levels = { debug: 0, info: 1, warning: 2, error: 3 };
    }
    
    _log(level, message, ...args) {
        if (this.levels[level] >= this.levels[this.level]) {
            const timestamp = new Date().toISOString();
            console.log(`${timestamp} - freecap_client - ${level.toUpperCase()} - ${message}`, ...args);
        }
    }
    
    debug(message, ...args) {
        this._log('debug', message, ...args);
    }
    
    info(message, ...args) {
        this._log('info', message, ...args);
    }
    
    warning(message, ...args) {
        this._log('warning', message, ...args);
    }
    
    error(message, ...args) {
        this._log('error', message, ...args);
    }
}

/**
 * No-op logger that discards all messages
 */
class NullLogger extends ILogger {
    debug() {}
    info() {}
    warning() {}
    error() {}
}

/**
 * Client configuration options
 */
class ClientConfig {
    constructor(options = {}) {
        this.baseUrl = options.baseUrl || "https://freecap.su";
        this.requestTimeout = options.requestTimeout || 30000;
        this.maxRetries = options.maxRetries || 3;
        this.retryDelay = options.retryDelay || 1000;
        this.defaultTaskTimeout = options.defaultTaskTimeout || 120;
        this.defaultCheckInterval = options.defaultCheckInterval || 3;
        this.userAgent = options.userAgent || "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";
    }
}

/**
 * Professional async client for FreeCap captcha solving service
 */
class FreeCapClient {
    /**
     * Initialize the FreeCap client
     * @param {string} apiKey - Your FreeCap API key
     * @param {ClientConfig} [config] - Client configuration options
     * @param {ILogger} [logger] - Logger instance
     */
    constructor(apiKey, config = null, logger = null) {
        if (!apiKey || !apiKey.trim()) {
            throw new FreeCapValidationException("API key cannot be empty");
        }
        
        this._apiKey = apiKey.trim();
        this._config = config || new ClientConfig();
        this._logger = logger || new ConsoleLogger();
        this._closed = false;
        
        if (!this._config.baseUrl.startsWith('http://') && !this._config.baseUrl.startsWith('https://')) {
            throw new FreeCapValidationException("API URL must start with http:// or https://");
        }
        
        this._baseUrl = this._config.baseUrl.replace(/\/$/, '');
        this._headers = {
            "X-API-Key": this._apiKey,
            "Content-Type": "application/json",
            "User-Agent": this._config.userAgent,
            "Accept": "application/json"
        };
    }
    
    /**
     * Validate task configuration for specific captcha type
     * @param {CaptchaTask} task - Task configuration
     * @param {string} captchaType - Captcha type
     */
    _validateTask(task, captchaType) {
        if (captchaType === CaptchaType.HCAPTCHA) {
            if (!task.sitekey) {
                throw new FreeCapValidationException("sitekey is required for hCaptcha");
            }
            if (!task.siteurl) {
                throw new FreeCapValidationException("siteurl is required for hCaptcha");
            }
            if (!task.groq_api_key) {
                throw new FreeCapValidationException("groq_api_key is required for hCaptcha");
            }
            if (!task.rqdata) {
                throw new FreeCapValidationException("rqdata is required for hCaptcha");
            }
        } else if (captchaType === CaptchaType.CAPTCHAFOX) {
            if (!task.sitekey) {
                throw new FreeCapValidationException("sitekey is required for CaptchaFox");
            }
            if (!task.siteurl) {
                throw new FreeCapValidationException("siteurl is required for CaptchaFox");
            }
        } else if (captchaType === CaptchaType.DISCORD_ID) {
            if (!task.sitekey) {
                throw new FreeCapValidationException("sitekey is required for Discord ID");
            }
            if (!task.siteurl) {
                throw new FreeCapValidationException("siteurl is required for Discord ID");
            }
        } else if (captchaType === CaptchaType.GEETEST) {
            if (!task.challenge) {
                throw new FreeCapValidationException("challenge is required for Geetest");
            }
        } else if (captchaType === CaptchaType.FUNCAPTCHA) {
            if (!task.preset) {
                throw new FreeCapValidationException("preset is required for FunCaptcha");
            }
            // More flexible chrome version validation
            const chromeVersion = parseInt(task.chrome_version);
            if (isNaN(chromeVersion) || chromeVersion < 100 || chromeVersion > 200) {
                throw new FreeCapValidationException("chrome_version must be a valid Chrome version number (e.g., 136, 137)");
            }
        }
    }
    
    /**
     * Build API payload for specific captcha type
     * @param {CaptchaTask} task - Task configuration
     * @param {string} captchaType - Captcha type
     * @returns {Object} API payload
     */
    _buildPayload(task, captchaType) {
        this._validateTask(task, captchaType);
        
        let payloadData = {};
        
        if (captchaType === CaptchaType.HCAPTCHA) {
            payloadData = {
                websiteURL: task.siteurl,
                websiteKey: task.sitekey,
                rqData: task.rqdata,
                groqApiKey: task.groq_api_key
            };
        } else if (captchaType === CaptchaType.CAPTCHAFOX) {
            payloadData = {
                websiteURL: task.siteurl,
                websiteKey: task.sitekey
            };
        } else if (captchaType === CaptchaType.GEETEST) {
            payloadData = {
                Challenge: task.challenge,
                RiskType: task.risk_type || RiskType.SLIDE
            };
        } else if (captchaType === CaptchaType.DISCORD_ID) {
            payloadData = {
                websiteURL: task.siteurl,
                websiteKey: task.sitekey
            };
        } else if (captchaType === CaptchaType.FUNCAPTCHA) {
            payloadData = {
                preset: task.preset,
                chrome_version: task.chrome_version,
                blob: task.blob
            };
        } else if (captchaType === CaptchaType.AURO_NETWORK) {
            payloadData = {};
        }
        
        if (task.proxy) {
            payloadData.proxy = task.proxy;
        }
        
        return {
            captchaType: captchaType,
            payload: payloadData
        };
    }
    
    /**
     * Make HTTP request with retries
     * @param {string} method - HTTP method
     * @param {string} endpoint - API endpoint
     * @param {Object} [data] - Request data
     * @param {number} [maxRetries] - Maximum retry attempts
     * @returns {Promise<Object>} Response data
     */
    async _makeRequest(method, endpoint, data = null, maxRetries = null) {
        if (this._closed) {
            throw new FreeCapException("Client has been closed");
        }
        
        if (maxRetries === null) {
            maxRetries = this._config.maxRetries;
        }
        
        const url = `${this._baseUrl}/${endpoint.replace(/^\//, '')}`;
        let lastException = null;
        
        for (let attempt = 0; attempt <= maxRetries; attempt++) {
            try {
                this._logger.debug(`Making ${method} request to ${url} (attempt ${attempt + 1})`);
                
                const options = {
                    method: method,
                    headers: this._headers,
                    timeout: this._config.requestTimeout
                };
                
                if (data) {
                    options.body = JSON.stringify(data);
                }
                
                const response = await fetch(url, options);
                const responseText = await response.text();
                
                let responseData;
                try {
                    responseData = responseText ? JSON.parse(responseText) : {};
                } catch (e) {
                    responseData = { raw_response: responseText };
                }
                
                if (response.status === 200) {
                    return responseData;
                }
                
                if (response.status === 401) {
                    throw new FreeCapAPIException(
                        "Invalid API key",
                        response.status,
                        responseData
                    );
                } else if (response.status === 429) {
                    throw new FreeCapAPIException(
                        "Rate limit exceeded",
                        response.status,
                        responseData
                    );
                } else if (response.status >= 500) {
                    const errorMsg = `Server error ${response.status}: ${responseText}`;
                    this._logger.warning(`${errorMsg} (attempt ${attempt + 1})`);
                    lastException = new FreeCapAPIException(
                        errorMsg,
                        response.status,
                        responseData
                    );
                } else {
                    throw new FreeCapAPIException(
                        `HTTP error ${response.status}: ${responseText}`,
                        response.status,
                        responseData
                    );
                }
                
            } catch (error) {
                if (error instanceof FreeCapAPIException) {
                    if (error.statusCode === 401 || error.statusCode === 429 || error.statusCode < 500) {
                        throw error;
                    }
                    lastException = error;
                } else {
                    const errorMsg = `Network error: ${error.message}`;
                    this._logger.warning(`${errorMsg} (attempt ${attempt + 1})`);
                    lastException = new FreeCapAPIException(errorMsg);
                }
            }
            
            if (attempt < maxRetries) {
                const delay = this._config.retryDelay * Math.pow(2, attempt);
                await this._sleep(delay);
            }
        }
        
        throw lastException || new FreeCapAPIException("Max retries exceeded");
    }
    
    /**
     * Sleep for specified milliseconds
     * @param {number} ms - Milliseconds to sleep
     * @returns {Promise<void>}
     */
    _sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
    
    /**
     * Create a captcha solving task
     * @param {CaptchaTask} task - Captcha task configuration
     * @param {string} captchaType - Type of captcha to solve
     * @returns {Promise<string>} Task ID
     */
    async createTask(task, captchaType) {
        const payload = this._buildPayload(task, captchaType);
        
        this._logger.info(`Creating ${captchaType} task for ${task.siteurl || 'N/A'}`);
        this._logger.debug(`Task payload: ${JSON.stringify(payload, null, 2)}`);
        
        const response = await this._makeRequest("POST", "/CreateTask", payload);
        
        if (!response.status) {
            const errorMsg = response.error || "Unknown error creating task";
            throw new FreeCapAPIException(`Failed to create task: ${errorMsg}`, null, response);
        }
        
        const taskId = response.taskId;
        if (!taskId) {
            throw new FreeCapAPIException("No task ID in response", null, response);
        }
        
        this._logger.info(`Task created successfully: ${taskId}`);
        return taskId;
    }
    
    /**
     * Get task result by ID
     * @param {string} taskId - Task ID to check
     * @returns {Promise<Object>} Task result
     */
    async getTaskResult(taskId) {
        if (!taskId || !taskId.trim()) {
            throw new FreeCapValidationException("Task ID cannot be empty");
        }
        
        const payload = { taskId: taskId.trim() };
        
        this._logger.debug(`Checking task status: ${taskId}`);
        
        const response = await this._makeRequest("POST", "/GetTask", payload);
        return response;
    }
    
    /**
     * Solve a captcha and return the solution
     * @param {CaptchaTask} task - Captcha task configuration
     * @param {string} captchaType - Type of captcha to solve
     * @param {number} [timeout] - Maximum time to wait for solution (seconds)
     * @param {number} [checkInterval] - Time between status checks (seconds)
     * @returns {Promise<string>} Captcha solution
     */
    async solveCaptcha(task, captchaType, timeout = null, checkInterval = null) {
        if (timeout === null) {
            timeout = this._config.defaultTaskTimeout;
        }
        if (checkInterval === null) {
            checkInterval = this._config.defaultCheckInterval;
        }
        
        if (timeout <= 0) {
            throw new FreeCapValidationException("Timeout must be positive");
        }
        if (checkInterval <= 0) {
            throw new FreeCapValidationException("Check interval must be positive");
        }
        
        const taskId = await this.createTask(task, captchaType);
        
        const startTime = Date.now();
        this._logger.info(`Waiting for task ${taskId} to complete (timeout: ${timeout}s)`);
        
        while (true) {
            const elapsedTime = (Date.now() - startTime) / 1000;
            const remainingTime = timeout - elapsedTime;
            
            if (remainingTime <= 0) {
                throw new FreeCapTimeoutException(`Task ${taskId} timed out after ${timeout} seconds`);
            }
            
            try {
                const result = await this.getTaskResult(taskId);
                const status = (result.status || "").toLowerCase();
                
                this._logger.debug(`Task ${taskId} status: ${status}`);
                
                if (status === TaskStatus.SOLVED) {
                    const solution = result.solution;
                    if (!solution) {
                        throw new FreeCapAPIException(
                            `Task ${taskId} marked as solved but no solution provided`,
                            null,
                            result
                        );
                    }
                    
                    this._logger.info(`Task ${taskId} solved successfully`);
                    return solution;
                } else if ([TaskStatus.ERROR, TaskStatus.FAILED].includes(status)) {
                    const errorMessage = result.error || result.Error || "Unknown error";
                    throw new FreeCapAPIException(
                        `Task ${taskId} failed: ${errorMessage}`,
                        null,
                        result
                    );
                } else if ([TaskStatus.PROCESSING, TaskStatus.PENDING].includes(status)) {
                    this._logger.debug(`Task ${taskId} still ${status}, ${Math.floor(remainingTime)}s remaining`);
                } else {
                    this._logger.warning(`Unknown task status for ${taskId}: ${status}`);
                }
                
            } catch (error) {
                if (error instanceof FreeCapTimeoutException || error instanceof FreeCapAPIException) {
                    throw error;
                }
                this._logger.warning(`Error checking task ${taskId}: ${error.message}`);
            }
            
            await this._sleep(checkInterval * 1000);
        }
    }
    
    /**
     * Close the client and cleanup resources
     */
    async close() {
        if (this._closed) {
            return;
        }
        
        this._closed = true;
        this._logger.debug("Client closed");
    }
}

/**
 * Convenience function to solve hCaptcha
 * @param {string} apiKey - FreeCap API key
 * @param {string} sitekey - Site key for hCaptcha
 * @param {string} siteurl - Website URL (should be discord.com for Discord)
 * @param {string} rqdata - rqData parameter (required for Discord)
 * @param {string} groqApiKey - Groq API key for solving
 * @param {string} [proxy] - Proxy string (optional)
 * @param {number} [timeout=120] - Timeout in seconds
 * @returns {Promise<string>} Captcha solution
 */
async function solveHCaptcha(apiKey, sitekey, siteurl, rqdata, groqApiKey, proxy = null, timeout = 120) {
    const client = new FreeCapClient(apiKey);
    try {
        const task = new CaptchaTask({
            sitekey: sitekey,
            siteurl: siteurl,
            rqdata: rqdata,
            groq_api_key: groqApiKey,
            proxy: proxy
        });
        return await client.solveCaptcha(task, CaptchaType.HCAPTCHA, timeout);
    } finally {
        await client.close();
    }
}

/**
 * Convenience function to solve FunCaptcha
 * @param {string} apiKey - FreeCap API key
 * @param {string} preset - FunCaptcha preset
 * @param {string} [chromeVersion="137"] - Chrome version (136 or 137)
 * @param {string} [blob="undefined"] - Blob parameter (required for Roblox presets)
 * @param {string} [proxy] - Proxy string (optional)
 * @param {number} [timeout=120] - Timeout in seconds
 * @returns {Promise<string>} Captcha solution
 */
async function solveFunCaptcha(apiKey, preset, chromeVersion = "137", blob = "undefined", proxy = null, timeout = 120) {
    const client = new FreeCapClient(apiKey);
    try {
        const task = new CaptchaTask({
            preset: preset,
            chrome_version: chromeVersion,
            blob: blob,
            proxy: proxy
        });
        return await client.solveCaptcha(task, CaptchaType.FUNCAPTCHA, timeout);
    } finally {
        await client.close();
    }
}

/**
 * Example usage of the FreeCap client
 */
async function main() {
    try {
        const client = new FreeCapClient("your-api-key");
        
        const task = new CaptchaTask({
            sitekey: "a9b5fb07-92ff-493f-86fe-352a2803b3df",
            siteurl: "discord.com",
            rqdata: "your-rq-data-here",
            groq_api_key: "your-groq-api-key",
            proxy: "http://user:pass@host:port"
        });
        
        const solution = await client.solveCaptcha(
            task,
            CaptchaType.HCAPTCHA,
            180
        );
        
        console.log(`✅ hCaptcha solved: ${solution}`);
        
        await client.close();
        
    } catch (error) {
        if (error instanceof FreeCapValidationException) {
            console.log(`❌ Validation error: ${error.message}`);
        } else if (error instanceof FreeCapTimeoutException) {
            console.log(`⏰ Timeout error: ${error.message}`);
        } else if (error instanceof FreeCapAPIException) {
            console.log(`🌐 API error: ${error.message}`);
            if (error.statusCode) {
                console.log(`   Status code: ${error.statusCode}`);
            }
            if (error.responseData) {
                console.log(`   Response: ${JSON.stringify(error.responseData)}`);
            }
        } else {
            console.log(`💥 Unexpected error: ${error.message}`);
        }
    }
}

// Export all classes and functions
module.exports = {
    CaptchaType,
    TaskStatus,
    RiskType,
    FunCaptchaPreset,
    CaptchaTask,
    FreeCapException,
    FreeCapAPIException,
    FreeCapTimeoutException,
    FreeCapValidationException,
    ILogger,
    ConsoleLogger,
    NullLogger,
    ClientConfig,
    FreeCapClient,
    solveHCaptcha,
    solveFunCaptcha,
    main
};

// Run example if this file is executed directly
if (require.main === module) {
    main().catch(console.error);
}
