/**
 * FreeCap Client - Usage Examples
 * 
 * This file demonstrates how to use the FreeCap client library
 * to solve various types of captchas.
 */

const { 
    FreeCapClient, 
    CaptchaTask, 
    CaptchaType, 
    FunCaptchaPreset,
    solveHCaptcha,
    solveFunCaptcha
} = require('../index');

/**
 * Example 1: Using the main client class to solve hCaptcha
 */
async function exampleHCaptchaWithClient() {
    console.log('🔄 Example 1: Solving hCaptcha with FreeCapClient...');
    
    const client = new FreeCapClient("your-api-key-here");
    
    try {
        const task = new CaptchaTask({
            sitekey: "a9b5fb07-92ff-493f-86fe-352a2803b3df",
            siteurl: "discord.com",
            rqdata: "your-rq-data-here",
            groq_api_key: "your-groq-api-key",
            proxy: "http://user:pass@host:port" // Optional
        });
        
        const solution = await client.solveCaptcha(
            task,
            CaptchaType.HCAPTCHA,
            180 // 3 minute timeout
        );
        
        console.log(`✅ hCaptcha solved: ${solution}`);
        
    } catch (error) {
        console.error(`❌ Error: ${error.message}`);
    } finally {
        await client.close();
    }
}

/**
 * Example 2: Using the convenience function for hCaptcha
 */
async function exampleHCaptchaConvenience() {
    console.log('🔄 Example 2: Solving hCaptcha with convenience function...');
    
    try {
        const solution = await solveHCaptcha(
            "your-api-key-here",
            "a9b5fb07-92ff-493f-86fe-352a2803b3df",
            "discord.com",
            "your-rq-data-here",
            "your-groq-api-key",
            null, // No proxy
            120   // 2 minute timeout
        );
        
        console.log(`✅ hCaptcha solved: ${solution}`);
        
    } catch (error) {
        console.error(`❌ Error: ${error.message}`);
    }
}

/**
 * Example 3: Solving FunCaptcha for Roblox
 */
async function exampleFunCaptcha() {
    console.log('🔄 Example 3: Solving FunCaptcha for Roblox...');
    
    try {
        const solution = await solveFunCaptcha(
            "your-api-key-here",
            FunCaptchaPreset.ROBLOX_LOGIN,
            "137", // Chrome version
            "your-blob-data-here",
            null, // No proxy
            180   // 3 minute timeout
        );
        
        console.log(`✅ FunCaptcha solved: ${solution}`);
        
    } catch (error) {
        console.error(`❌ Error: ${error.message}`);
    }
}

/**
 * Example 4: Using custom configuration and logging
 */
async function exampleWithCustomConfig() {
    console.log('🔄 Example 4: Using custom configuration...');
    
    const { ClientConfig, ConsoleLogger } = require('../index');
    
    const config = new ClientConfig({
        timeout: 30000,      // 30 second request timeout
        maxRetries: 5,       // Retry failed requests 5 times
        defaultTimeout: 300  // 5 minute default solve timeout
    });
    
    const logger = new ConsoleLogger('debug'); // Enable debug logging
    
    const client = new FreeCapClient("your-api-key-here", config, logger);
    
    try {
        const task = new CaptchaTask({
            sitekey: "your-sitekey",
            siteurl: "your-site-url"
        });
        
        const solution = await client.solveCaptcha(task, CaptchaType.HCAPTCHA);
        console.log(`✅ Solution: ${solution}`);
        
    } catch (error) {
        console.error(`❌ Error: ${error.message}`);
    } finally {
        await client.close();
    }
}

/**
 * Run all examples
 */
async function runExamples() {
    console.log('🚀 FreeCap Client Examples\n');
    
    // Note: These examples will fail without valid API keys and captcha data
    // They are provided to demonstrate the API usage
    
    console.log('⚠️  Note: Replace API keys and captcha data with real values to test\n');
    
    try {
        await exampleHCaptchaWithClient();
        console.log();
        
        await exampleHCaptchaConvenience();
        console.log();
        
        await exampleFunCaptcha();
        console.log();
        
        await exampleWithCustomConfig();
        
    } catch (error) {
        console.error('💥 Unexpected error:', error.message);
    }
    
    console.log('\n✨ Examples completed!');
}

// Run examples if this file is executed directly
if (require.main === module) {
    runExamples().catch(console.error);
}

module.exports = {
    exampleHCaptchaWithClient,
    exampleHCaptchaConvenience,
    exampleFunCaptcha,
    exampleWithCustomConfig,
    runExamples
}; 