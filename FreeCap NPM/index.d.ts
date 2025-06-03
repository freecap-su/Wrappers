/**
 * FreeCap API Client - TypeScript Definitions
 */

export declare enum CaptchaType {
    HCAPTCHA = "hcaptcha",
    CAPTCHAFOX = "captchafox",
    GEETEST = "geetest",
    DISCORD_ID = "discordid",
    FUNCAPTCHA = "funcaptcha",
    AURO_NETWORK = "auronetwork"
}

export declare enum TaskStatus {
    PENDING = "pending",
    PROCESSING = "processing",
    SOLVED = "solved",
    ERROR = "error",
    FAILED = "failed"
}

export declare enum RiskType {
    SLIDE = "slide",
    GOBANG = "gobang",
    ICON = "icon",
    AI = "ai"
}

export declare enum FunCaptchaPreset {
    ROBLOX_LOGIN = "roblox_login",
    ROBLOX_FOLLOW = "roblox_follow",
    ROBLOX_GROUP = "roblox_group",
    DROPBOX_LOGIN = "dropbox_login"
}

export interface CaptchaTaskOptions {
    sitekey?: string;
    siteurl?: string;
    proxy?: string;
    rqdata?: string;
    groq_api_key?: string;
    challenge?: string;
    risk_type?: string;
    preset?: string;
    chrome_version?: string;
    blob?: string;
}

export declare class CaptchaTask {
    sitekey: string | null;
    siteurl: string | null;
    proxy: string | null;
    rqdata: string | null;
    groq_api_key: string | null;
    challenge: string | null;
    risk_type: string | null;
    preset: string | null;
    chrome_version: string;
    blob: string;
    
    constructor(options?: CaptchaTaskOptions);
}

export declare class FreeCapException extends Error {
    name: string;
    constructor(message: string);
}

export declare class FreeCapAPIException extends FreeCapException {
    statusCode: number | null;
    responseData: any;
    constructor(message: string, statusCode?: number | null, responseData?: any);
}

export declare class FreeCapTimeoutException extends FreeCapException {
    constructor(message: string);
}

export declare class FreeCapValidationException extends FreeCapException {
    constructor(message: string);
}

export declare abstract class ILogger {
    abstract debug(message: string, ...args: any[]): void;
    abstract info(message: string, ...args: any[]): void;
    abstract warning(message: string, ...args: any[]): void;
    abstract error(message: string, ...args: any[]): void;
}

export declare class ConsoleLogger extends ILogger {
    level: string;
    constructor(level?: string);
    debug(message: string, ...args: any[]): void;
    info(message: string, ...args: any[]): void;
    warning(message: string, ...args: any[]): void;
    error(message: string, ...args: any[]): void;
}

export declare class NullLogger extends ILogger {
    debug(): void;
    info(): void;
    warning(): void;
    error(): void;
}

export interface ClientConfigOptions {
    baseUrl?: string;
    requestTimeout?: number;
    maxRetries?: number;
    retryDelay?: number;
    defaultTaskTimeout?: number;
    defaultCheckInterval?: number;
    userAgent?: string;
}

export declare class ClientConfig {
    baseUrl: string;
    requestTimeout: number;
    maxRetries: number;
    retryDelay: number;
    defaultTaskTimeout: number;
    defaultCheckInterval: number;
    userAgent: string;
    
    constructor(options?: ClientConfigOptions);
}

export declare class FreeCapClient {
    constructor(apiKey: string, config?: ClientConfig | null, logger?: ILogger | null);
    
    createTask(task: CaptchaTask, captchaType: CaptchaType): Promise<string>;
    getTaskResult(taskId: string): Promise<any>;
    solveCaptcha(task: CaptchaTask, captchaType: CaptchaType, timeout?: number | null, checkInterval?: number | null): Promise<string>;
    close(): Promise<void>;
}

export declare function solveHCaptcha(
    apiKey: string,
    sitekey: string,
    siteurl: string,
    rqdata: string,
    groqApiKey: string,
    proxy?: string | null,
    timeout?: number
): Promise<string>;

export declare function solveFunCaptcha(
    apiKey: string,
    preset: string,
    chromeVersion?: string,
    blob?: string,
    proxy?: string | null,
    timeout?: number
): Promise<string>;

// Default export
declare const _default: typeof FreeCapClient;
export default _default; 