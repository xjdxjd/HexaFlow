using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HexaFlow.Services
{
    /// <summary>
    /// 应用程序配置服务，负责加载和保存配置文件
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath;
        private readonly string _modelsPath;
        private AppConfiguration _config;
        private ModelsConfiguration _modelsConfig;

        public ConfigService()
        {
            // 确保应用程序目录存在
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "HexaFlow");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _configPath = Path.Combine(appDataPath, "config.json");
            _modelsPath = Path.Combine(appDataPath, "models.json");
        }

        /// <summary>
        /// 异步加载配置文件
        /// </summary>
        public async Task LoadConfigurationsAsync()
        {
            // 加载应用程序配置
            if (File.Exists(_configPath))
            {
                try
                {
                    string configJson = await File.ReadAllTextAsync(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfiguration>(configJson) ?? new AppConfiguration();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {ex.Message}");
                    _config = new AppConfiguration();
                }
            }
            else
            {
                _config = new AppConfiguration();
                await SaveConfigAsync();
            }

            // 加载模型配置
            if (File.Exists(_modelsPath))
            {
                try
                {
                    string modelsJson = await File.ReadAllTextAsync(_modelsPath);
                    _modelsConfig = JsonSerializer.Deserialize<ModelsConfiguration>(modelsJson) ?? new ModelsConfiguration();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载模型配置文件失败: {ex.Message}");
                    _modelsConfig = new ModelsConfiguration();
                }
            }
            else
            {
                _modelsConfig = new ModelsConfiguration();
                await SaveModelsConfigAsync();
            }
        }

        /// <summary>
        /// 保存应用程序配置
        /// </summary>
        public async Task SaveConfigAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string configJson = JsonSerializer.Serialize(_config, options);
                await File.WriteAllTextAsync(_configPath, configJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存模型配置
        /// </summary>
        public async Task SaveModelsConfigAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string modelsJson = JsonSerializer.Serialize(_modelsConfig, options);
                await File.WriteAllTextAsync(_modelsPath, modelsJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存模型配置文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        public AppConfiguration Config => _config;

        /// <summary>
        /// 获取模型配置
        /// </summary>
        public ModelsConfiguration ModelsConfig => _modelsConfig;
    }

    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Ollama API地址
        /// </summary>
        public string OllamaApiUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// 默认模型
        /// </summary>
        public string DefaultModel { get; set; } = "";

        /// <summary>
        /// 主题设置
        /// </summary>
        public string Theme { get; set; } = "Light";

        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// 字体设置
        /// </summary>
        public string FontFamily { get; set; } = "Microsoft YaHei UI";

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; set; } = 16;

        /// <summary>
        /// 自动保存聊天记录
        /// </summary>
        public bool AutoSaveChatHistory { get; set; } = true;

        /// <summary>
        /// 聊天记录保存位置
        /// </summary>
        public string ChatHistoryPath { get; set; } = "";

        /// <summary>
        /// 系统提示词位置
        /// </summary>
        public string SystemPromptsPath { get; set; } = "";
    }

    /// <summary>
    /// 模型配置
    /// </summary>
    public class ModelsConfiguration
    {
        /// <summary>
        /// 默认温度参数
        /// </summary>
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// 默认Top P参数
        /// </summary>
        public double DefaultTopP { get; set; } = 0.9;

        /// <summary>
        /// 默认Top K参数
        /// </summary>
        public int DefaultTopK { get; set; } = 40;

        /// <summary>
        /// 默认最大Token数
        /// </summary>
        public int DefaultMaxTokens { get; set; } = 2048;

        /// <summary>
        /// 模型特定参数
        /// </summary>
        public System.Collections.Generic.Dictionary<string, ModelParameters> ModelSpecificParameters { get; set; } = 
            new System.Collections.Generic.Dictionary<string, ModelParameters>();
    }

    /// <summary>
    /// 模型特定参数
    /// </summary>
    public class ModelParameters
    {
        /// <summary>
        /// 温度参数
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Top P参数
        /// </summary>
        public double? TopP { get; set; }

        /// <summary>
        /// Top K参数
        /// </summary>
        public int? TopK { get; set; }

        /// <summary>
        /// 最大Token数
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// 系统提示词
        /// </summary>
        public string SystemPrompt { get; set; } = "";
    }
}
