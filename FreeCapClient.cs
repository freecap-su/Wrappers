using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FreeCap
{
    public class CaptchaTask
    {
        public string Sitekey { get; set; }
        public string Siteurl { get; set; }
        public string Proxy { get; set; }
        public string Rqdata { get; set; }

        public CaptchaTask(string sitekey, string siteurl, string proxy, string rqdata = null)
        {
            Sitekey = sitekey;
            Siteurl = siteurl;
            Proxy = proxy;
            Rqdata = rqdata;
        }
    }

    public class FreeCapClient
    {
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public FreeCapClient(string apiKey, string apiUrl = "https://freecap.app", ILogger logger = null)
        {
            _apiKey = apiKey;
            _apiUrl = apiUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _logger = logger ?? new ConsoleLogger();
        }

        public async Task<Dictionary<string, object>> CreateTaskAsync(
            CaptchaTask task,
            string captchaType = "hcaptcha")
        {
            var taskData = new Dictionary<string, object>
            {
                ["freecap_key"] = _apiKey,
                ["captcha_type"] = captchaType,
                ["payload"] = new Dictionary<string, string>
                {
                    ["sitekey"] = task.Sitekey,
                    ["siteurl"] = task.Siteurl
                }
            };

            var payload = taskData["payload"] as Dictionary<string, string>;

            if (!string.IsNullOrEmpty(task.Proxy))
            {
                payload["proxy"] = task.Proxy;
            }

            if (!string.IsNullOrEmpty(task.Rqdata) && captchaType == "hcaptcha")
            {
                payload["rqdata"] = task.Rqdata;
            }

            _logger.Info($"Creating {captchaType} task for site: {task.Siteurl}");
            
            var content = new StringContent(
                JsonSerializer.Serialize(taskData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/create_task", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!(bool)result["success"] || !result.ContainsKey("task_id"))
            {
                throw new Exception($"Error creating task: {responseContent}");
            }

            return result;
        }

        public async Task<Dictionary<string, object>> GetResultAsync(string taskId)
        {
            var requestData = new Dictionary<string, string>
            {
                ["freecap_key"] = _apiKey,
                ["task_id"] = taskId
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/get_task", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Dictionary<string, object>>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<string> SolveCaptchaAsync(
            CaptchaTask task,
            string captchaType = "hcaptcha",
            int timeout = 120,
            int checkInterval = 3)
        {
            var taskResult = await CreateTaskAsync(task, captchaType);
            var taskId = taskResult["task_id"].ToString();

            var startTime = DateTime.Now;
            while (true)
            {
                if ((DateTime.Now - startTime).TotalSeconds > timeout)
                {
                    throw new TimeoutException($"Task {taskId} timed out after {timeout} seconds");
                }

                var result = await GetResultAsync(taskId);

                if (result["status"].ToString() == "solved")
                {
                    _logger.Info($"Task {taskId} solved successfully");
                    return result["captcha_token"].ToString();
                }
                else if (result["status"].ToString() == "error")
                {
                    throw new Exception($"Task {taskId} failed: {result.ContainsKey("error") ? result["error"] : "Unknown error"}");
                }

                await Task.Delay(checkInterval * 1000);
            }
        }
    }

    public interface ILogger
    {
        void Info(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }
    }
}