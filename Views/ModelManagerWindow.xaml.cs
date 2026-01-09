using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AIChat.Views;
using OllamaSharp;
using OllamaSharp.Models;

namespace HexaFlow.Views
{
    /// <summary>
    /// ModelManagerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ModelManagerWindow : Window, INotifyPropertyChanged
    {
        private readonly OllamaApiClient _ollamaClient;
        private string _searchText = "";
        private string _selectedCategory = "all";
        private string _selectedSort = "popularity";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public ObservableCollection<ModelInfo> AvailableModels { get; set; } = new ObservableCollection<ModelInfo>();
        public ObservableCollection<Model> DownloadedModels { get; set; } = new ObservableCollection<Model>();

        public ModelManagerWindow(OllamaApiClient ollamaClient)
        {
            InitializeComponent();
            _ollamaClient = ollamaClient;
            DataContext = this;

            // 初始化时加载数据
            Loaded += ModelManagerWindow_Loaded;
        }

        private async void ModelManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_ollamaClient == null)
            {
                MessageBox.Show("Ollama客户端未初始化，无法加载模型列表", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            await LoadAvailableModelsAsync();
            await LoadDownloadedModelsAsync();
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                if (_ollamaClient == null)
                {
                    StatusTextBlock.Text = "Ollama客户端未初始化，无法加载可用模型列表";
                    return;
                }
                
                // 这里应该调用Ollama API获取可用模型列表
                // 由于OllamaSharp可能不直接提供搜索功能，这里使用模拟数据
                // 实际实现中可能需要调用Ollama的搜索API或使用其他数据源

                AvailableModels.Clear();

                // 模拟数据 - 实际应用中应该从API获取
                var mockModels = new[]
                {
                    new ModelInfo 
                    { 
                        Name = "llama2", 
                        Tags = "对话", 
                        Size = "3.8GB", 
                        PullCount = "5.2M",
                        Popularity = 4.5,
                        Description = "Meta的Llama 2模型，适合对话和文本生成"
                    },
                    new ModelInfo 
                    { 
                        Name = "codellama", 
                        Tags = "代码", 
                        Size = "3.8GB", 
                        PullCount = "2.1M",
                        Popularity = 4.2,
                        Description = "专门用于代码生成和理解的模型"
                    },
                    new ModelInfo 
                    { 
                        Name = "mistral", 
                        Tags = "对话", 
                        Size = "4.1GB", 
                        PullCount = "3.7M",
                        Popularity = 4.3,
                        Description = "Mistral AI的对话模型"
                    },
                    new ModelInfo 
                    { 
                        Name = "vicuna", 
                        Tags = "对话", 
                        Size = "3.0GB", 
                        PullCount = "1.8M",
                        Popularity = 4.0,
                        Description = "基于Llama的微调对话模型"
                    },
                    new ModelInfo 
                    { 
                        Name = "all-minilm", 
                        Tags = "嵌入", 
                        Size = "0.2GB", 
                        PullCount = "0.9M",
                        Popularity = 3.8,
                        Description = "轻量级嵌入模型"
                    }
                };

                foreach (var model in mockModels)
                {
                    AvailableModels.Add(model);
                }

                AvailableModelsListView.ItemsSource = AvailableModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载可用模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDownloadedModelsAsync()
        {
            try
            {
                if (_ollamaClient == null)
                {
                    StatusTextBlock.Text = "Ollama客户端未初始化，无法加载模型列表";
                    return;
                }
                
                DownloadedModels.Clear();

                // 调用Ollama API获取已下载模型
                var models = await _ollamaClient.ListLocalModelsAsync();

                foreach (var model in models)
                {
                    DownloadedModels.Add(model);
                }

                DownloadedModelsListView.ItemsSource = DownloadedModels;

                // 计算总占用空间
                UpdateTotalSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载已下载模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotalSize()
        {
            // 这里应该计算所有模型的总大小
            // 由于Model类可能不直接提供大小信息，这里使用模拟数据
            long totalSize = DownloadedModels.Count * 4 * 1024 * 1024 * 1024; // 假设每个模型4GB
            TotalSizeTextBlock.Text = $"总占用空间: {FormatFileSize(totalSize)}";
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

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await FilterModelsAsync();
        }

        private async Task FilterModelsAsync()
        {
            try
            {
                // 这里应该根据搜索文本和分类筛选模型
                // 实际实现中可能需要调用API或对本地数据进行筛选

                // 简单的本地筛选示例
                var filtered = AvailableModels.Where(m => 
                    (string.IsNullOrEmpty(SearchText) || m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) &&
                    (_selectedCategory == "all" || m.Tags.Contains(_selectedCategory, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                
                if (filtered.Count > 0) {
                    // 应用排序
                    switch (_selectedSort)
                    {
                        case "popularity":
                            filtered = filtered.OrderByDescending(m => m.Popularity).ToList();
                            break;
                        case "size-asc":
                            filtered = filtered.OrderBy(m => m.SizeInBytes).ToList();
                            break;
                        case "size-desc":
                            filtered = filtered.OrderByDescending(m => m.SizeInBytes).ToList();
                            break;
                        case "name":
                            filtered = filtered.OrderBy(m => m.Name).ToList();
                            break;
                    }
                    AvailableModelsListView.ItemsSource = filtered;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"筛选模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item)
            {
                _selectedSort = item.Tag?.ToString() ?? "popularity";
                await FilterModelsAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAvailableModelsAsync();
            await LoadDownloadedModelsAsync();
        }

        private void ModelDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ModelInfo model)
            {
                // 显示模型详情
                MessageBox.Show(model.Description, $"{model.Name} 详情", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ModelInfo model)
            {
                try
                {
                    // 创建下载进度窗口
                    var progressDialog = new ModelDownloadProgressDialog(model.Name);
                    progressDialog.Owner = this;
                    progressDialog.Show();

                    // 开始下载模型
                    await Task.Run(async () =>
                    {
                        try
                        {
                            // 这里应该调用Ollama API下载模型
                            // 由于OllamaSharp可能不直接提供进度回调，这里使用模拟进度
                            for (int progress = 0; progress <= 100; progress += 5)
                            {
                                await Task.Delay(200); // 模拟下载时间
                                Dispatcher.Invoke(() => progressDialog.UpdateProgress(progress));
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => progressDialog.Close());
                            Dispatcher.Invoke(() => MessageBox.Show($"下载模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                            return;
                        }
                    });

                    progressDialog.Close();

                    // 刷新已下载模型列表
                    await LoadDownloadedModelsAsync();

                    MessageBox.Show($"模型 {model.Name} 下载完成!", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadedModelsListView.SelectedItem is Model selectedModel)
            {
                var result = MessageBox.Show($"确定要删除模型 {selectedModel.Name} 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 调用Ollama API删除模型
                        await _ollamaClient.DeleteModelAsync(new DeleteModelRequest { Model = selectedModel.Name });

                        // 刷新已下载模型列表
                        await LoadDownloadedModelsAsync();

                        MessageBox.Show($"模型 {selectedModel.Name} 已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"删除模型失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的模型", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void CleanSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            // 实现清理空间功能
            MessageBox.Show("清理空间功能将在后续版本中实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UseModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Model model)
            {
                // 设置当前使用的模型
                _ollamaClient.SelectedModel = model.Name;
                MessageBox.Show($"已切换到模型: {model.Name}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                // 不关闭窗口，让用户可以继续操作
                // Close();
            }
        }

        private void ModelInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Model model)
            {
                // 显示模型详细信息
                var infoWindow = new ModelInfoWindow(model);
                infoWindow.Owner = this;
                infoWindow.ShowDialog();
            }
        }

        private void ImportModelButton_Click(object sender, RoutedEventArgs e)
        {
            // 实现导入模型功能
            MessageBox.Show("导入模型功能将在后续版本中实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 模型信息类
    public class ModelInfo
    {
        public string Name { get; set; }
        public string Tags { get; set; }
        public string Size { get; set; }
        public long SizeInBytes 
        { 
            get
            {
                // 将大小字符串转换为字节数
                if (Size.EndsWith("GB"))
                    return long.Parse(Size.Replace("GB", "")) * 1024 * 1024 * 1024;
                if (Size.EndsWith("MB"))
                    return long.Parse(Size.Replace("MB", "")) * 1024 * 1024;
                if (Size.EndsWith("KB"))
                    return long.Parse(Size.Replace("KB", "")) * 1024;
                return long.Parse(Size);
            }
        }
        public string PullCount { get; set; }
        public double Popularity { get; set; }
        public string Description { get; set; }
    }
}
