/**
 * FreeCap API Client - Professional JavaScript Implementation
 * 
 * A robust, production-ready async client for the FreeCap captcha solving service.
 * Supports all captcha types including hCaptcha, FunCaptcha, Geetest, and more.
 * 
 * @author FreeCap Client
 * @version 1.0.0
 * @license GPL-3.0
 */

const {
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
    solveFunCaptcha
} = require('./lib/FreeCapClient');

// Main exports
module.exports = {
    // Main client class
    FreeCapClient,
    
    // Task and configuration classes
    CaptchaTask,
    ClientConfig,
    
    // Constants
    CaptchaType,
    TaskStatus,
    RiskType,
    FunCaptchaPreset,
    
    // Exceptions
    FreeCapException,
    FreeCapAPIException,
    FreeCapTimeoutException,
    FreeCapValidationException,
    
    // Loggers
    ILogger,
    ConsoleLogger,
    NullLogger,
    
    // Convenience functions
    solveHCaptcha,
    solveFunCaptcha
};

// Default export for ES6 compatibility
module.exports.default = FreeCapClient; 