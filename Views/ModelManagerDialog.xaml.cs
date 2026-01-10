using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HexaFlow.Services;

namespace HexaFlow.Views
{
    public partial class ModelManagerDialog : Window
    {
        private OllamaService _ollamaService;
        private ObservableCollection<ModelInfo> _models;

        public ModelManagerDialog()
        {
            InitializeComponent();
            _ollamaService = new OllamaService();
            _models = new ObservableCollection<ModelInfo>();
            ModelsListBox.ItemsSource = _models;

            // 绑定事件
            PullModelButton.Click += PullModelButton_Click;
            Loaded += ModelManagerDialog_Loaded;
        }

        private async void ModelManagerDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadModelsAsync();
        }

        private async Task LoadModelsAsync()
        {
            try
            {
                // 显示加载状态
                ModelsListBox.IsEnabled = false;
                PullModelButton.IsEnabled = false;

                // 检查 Ollama 服务是否可用
                bool isAvailable = await _ollamaService.IsAvailableAsync();
                if (!isAvailable)
                {
                    MessageBox.Show("无法连接到 Ollama 服务，请确保服务正在运行。", "连接错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 获取模型列表
                var models = await _ollamaService.GetLocalModelsAsync();

                // 更新UI
                _models.Clear();
                foreach (var model in models)
                {
                    _models.Add(model);
                }

                // 更新状态栏
                StatusTextBlock.Text = $"已加载 {_models.Count} 个模型";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载模型列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ModelsListBox.IsEnabled = true;
                PullModelButton.IsEnabled = true;
            }
        }

        private async void PullModelButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个简单的输入对话框
            var dialog = new InputDialog("请输入要拉取的模型名称（如 llama3）:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                try
                {
                    PullModelButton.IsEnabled = false;
                    PullModelButton.Content = "拉取中...";

                    // 创建进度报告器
                    var progress = new Progress<string>(progressText =>
                    {
                        PullModelButton.Content = $"拉取中: {progressText}";
                    });

                    // 拉取模型
                    await _ollamaService.PullModelAsync(dialog.InputText, progress);

                    // 刷新列表
                    await LoadModelsAsync();

                    MessageBox.Show($"模型 {dialog.InputText} 拉取成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"拉取模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    PullModelButton.IsEnabled = true;
                    PullModelButton.Content = "拉取新模型";
                }
            }
        }

        private async void DeleteModel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ModelInfo model)
            {
                var result = MessageBox.Show($"确定要删除模型 {model.Name} 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _ollamaService.DeleteModelAsync(model.Name);
                        await LoadModelsAsync();
                        MessageBox.Show($"模型 {model.Name} 删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    // 简单的输入对话框
    public class InputDialog : Window
    {
        public string InputText { get; private set; } = string.Empty;

        public InputDialog(string prompt)
        {
            Title = "输入";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var textBlock = new TextBlock { Text = prompt, Margin = new Thickness(10) };
            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            var textBox = new TextBox { Margin = new Thickness(10) };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            okButton.Click += (s, e) => { InputText = textBox.Text; DialogResult = true; Close(); };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button { Content = "取消", Width = 80 };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(buttonPanel);
            Content = grid;

            textBox.Focus();
        }
    }
}