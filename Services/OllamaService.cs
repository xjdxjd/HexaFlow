using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace HexaFlow.Services
{
    /// <summary>
    /// Ollama 服务类，用于与 Ollama API 交互
    /// </summary>
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public OllamaService(string baseUrl = "http://localhost:11434")
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // 设置较长的超时时间
        }

        /// <summary>
        /// 获取本地已安装的模型列表
        /// </summary>
        public async Task<List<ModelInfo>> GetLocalModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var models = new List<ModelInfo>();

                    if (doc.RootElement.TryGetProperty("models", out var modelsElement))
                    {
                        foreach (var model in modelsElement.EnumerateArray())
                        {
                            models.Add(new ModelInfo
                            {
                                Name = model.GetProperty("name").GetString(),
                                Size = FormatSize(model.GetProperty("size").GetInt64()),
                                Digest = model.GetProperty("digest").GetString(),
                                ModifiedAt = DateTime.Parse(model.GetProperty("modified_at").GetString())
                            });
                        }
                    }

                    return models;
                }
                else
                {
                    throw new Exception($"获取模型列表失败: {jsonContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"获取模型列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 拉取新模型
        /// </summary>
        public async Task PullModelAsync(string modelName, IProgress<string>? progress = null)
        {
            try
            {
                var payload = new { name = modelName };
                var jsonString = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/pull");
                request.Content = content;

                using var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"拉取模型失败: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"拉取模型 {modelName} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除模型
        /// </summary>
        public async Task DeleteModelAsync(string modelName)
        {
            try
            {
                var payload = new { name = modelName };
                var jsonString = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/delete");
                request.Content = content;

                using var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"删除模型失败: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"删除模型 {modelName} 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查 Ollama 服务是否可用
        /// </summary>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/version");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 发送聊天消息并返回响应
        /// </summary>
        public async Task<string> ChatAsync(string message, string model, List<MessageContent> history = null, Dictionary<string, object> parameters = null)
        {
            try
            {
                // 构建消息历史
                var messages = new List<object>();
                
                // 添加历史消息
                if (history != null)
                {
                    foreach (var msg in history)
                    {
                        messages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                
                // 添加当前用户消息
                messages.Add(new { role = "user", content = message });

                // 构造请求体
                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    stream = false, // 非流式响应
                    options = parameters // 添加参数选项
                };

                var jsonString = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    if (doc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        if (messageElement.TryGetProperty("content", out var contentElement))
                        {
                            return contentElement.GetString();
                        }
                    }
                    throw new Exception("API响应格式不正确");
                }
                else
                {
                    throw new Exception($"聊天请求失败: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"聊天请求失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 发送聊天消息并返回流式响应
        /// </summary>
        public async Task GenerateStreamAsync(
            string model, 
            List<Dictionary<string, string>> messages, 
            Action<string> onNewToken, 
            Dictionary<string, object> parameters = null)
        {
            try
            {
                // 构造请求体
                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    stream = true, // 流式响应
                    options = parameters // 添加参数选项
                };

                var jsonString = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    if (line.StartsWith("data: "))
                    {
                        var jsonData = line.Substring(6);
                        
                        try
                        {
                            using var doc = JsonDocument.Parse(jsonData);
                            if (doc.RootElement.TryGetProperty("done", out var doneElement) && doneElement.GetBoolean())
                            {
                                // 流结束
                                break;
                            }
                            
                            if (doc.RootElement.TryGetProperty("message", out var messageElement))
                            {
                                if (messageElement.TryGetProperty("content", out var contentElement))
                                {
                                    var tokenContent = contentElement.GetString();
                                    onNewToken?.Invoke(tokenContent);
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // 忽略JSON解析错误
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"流式聊天请求失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// 模型信息类
    /// </summary>
    public class ModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
        public DateTime ModifiedAt { get; set; }
        public object Details { get; set; } = new object();
    }

    /// <summary>
    /// 消息内容类
    /// </summary>
    public class MessageContent
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}