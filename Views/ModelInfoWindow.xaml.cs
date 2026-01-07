using System;
using System.Threading.Tasks;
using System.Windows;
using OllamaSharp;
using OllamaSharp.Models;

namespace HexaFlow.Views
{
    /// <summary>
    /// ModelInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ModelInfoWindow : Window
    {
        private readonly OllamaApiClient _ollamaClient;
        private readonly Model _model;

        public ModelInfoWindow(Model model, OllamaApiClient ollamaClient = null)
        {
            InitializeComponent();
            _model = model;
            _ollamaClient = ollamaClient;
            DataContext = this;

            // 显示基本信息
            ModelNameTextBlock.Text = _model.Name;

            Loaded += ModelInfoWindow_Loaded;
        }

        private async void ModelInfoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadModelDetailsAsync();
        }

        private async Task LoadModelDetailsAsync()
        {
            try
            {
                // 调用Ollama API获取模型详细信息
                var response = await _ollamaClient.ShowModelAsync(new ShowModelRequest { Model = _model.Name });

                if (response != null)
                {
                    // 显示模型大小
                    ModelSizeTextBlock.Text = FormatFileSize(0); // OllamaSharp API可能不直接提供大小信息
                    ParameterSizeTextBlock.Text = "未知"; // OllamaSharp API可能不直接提供参数量
                    ContextLengthTextBlock.Text = "未知"; // OllamaSharp API可能不直接提供上下文长度
                    QuantizationLevelTextBlock.Text = "未知"; // OllamaSharp API可能不直接提供量化方式
                    LicenseTextBlock.Text = "未知"; // OllamaSharp API可能不直接提供许可证
                    ModifiedAtTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 使用当前时间

                    // 显示Modelfile
                    if (!string.IsNullOrEmpty(response.Modelfile))
                    {
                        ModelfileTextBox.Text = response.Modelfile;
                    }
                    else
                    {
                        ModelfileTextBox.Text = "无Modelfile信息";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载模型详情失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatFileSize(long bytes)
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
