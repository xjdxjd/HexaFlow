
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HexaFlow.Models;
using HexaFlow.Services;
using HexaFlow.Views;
using OllamaSharp;
using OllamaSharp.Models;

namespace HexaFlow
{
    public partial class MainWindow : Window
    {
        private OllamaApiClient _ollama;
        private readonly ChatHistoryService _chatHistoryService;
        private readonly SystemPromptService _systemPromptService;
        private readonly ObservableCollection<ChatMessage> _chatMessages = new();
        private readonly ObservableCollection<ChatSession> _chatSessions = new();
        private readonly ObservableCollection<SystemPrompt> _systemPrompts = new();
        private Chat? _currentChat; // 当前会话（维护上下文）
        private int _currentSessionId = -1; // 当前会话ID，-1表示新会话
        private SystemPrompt? _currentSystemPrompt; // 当前使用的系统提示词

        public MainWindow()
        {
            InitializeComponent();
            ChatItemsControl.ItemsSource = _chatMessages;

            // 初始化历史服务
            _chatHistoryService = new ChatHistoryService();

            // 初始化系统提示词服务
            _systemPromptService = new SystemPromptService();

            // 暂时使用默认API地址，将在Loaded事件中从配置加载
            _ollama = new OllamaApiClient("http://localhost:11434");
            
            // 添加文本框事件绑定
            TemperatureValueText.TextChanged += ParameterTextBox_TextChanged;
            TopPValueText.TextChanged += ParameterTextBox_TextChanged;
            TopKValueText.TextChanged += ParameterTextBox_TextChanged;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 从配置中更新API地址
            if (App.ConfigService?.Config != null)
            {
                _ollama = new OllamaApiClient(App.ConfigService.Config.OllamaApiUrl);

                // 加载配置中的字体设置
                ApplyFontSettings(App.ConfigService.Config.FontFamily, App.ConfigService.Config.FontSize);
            }

            await LoadModelsAsync();
            await LoadChatHistoryAsync();
            await LoadSystemPromptsAsync();

            // 加载配置中的模型参数
            LoadModelParametersFromConfig();
        }

        // 应用字体设置
        private void ApplyFontSettings(string fontFamily, int fontSize)
        {
            try
            {
                // 更新主窗口样式
                var style = new Style(typeof(Window));
                style.Setters.Add(new Setter(Window.FontFamilyProperty, new FontFamily(fontFamily)));
                style.Setters.Add(new Setter(Window.FontSizeProperty, (double)fontSize));
                this.Style = style;

                // 更新主窗口中的特定控件字体
                UpdateControlFonts(this, fontFamily, fontSize);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用字体设置时出错: {ex.Message}");
            }
        }

        // 递归更新控件字体
        private void UpdateControlFonts(DependencyObject parent, string fontFamily, int fontSize)
        {
            if (parent == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement element)
                {
                    // 更新TextBlock字体
                    if (element is TextBlock textBlock)
                    {
                        textBlock.FontFamily = new FontFamily(fontFamily);

                        // 只更新默认大小的字体，避免破坏特意设置的字体大小
                        if (Math.Abs(textBlock.FontSize - 14) < 0.1 || Math.Abs(textBlock.FontSize - 16) < 0.1)
                        {
                            textBlock.FontSize = fontSize;
                        }
                    }
                    // 更新TextBox字体
                    else if (element is TextBox textBox)
                    {
                        textBox.FontFamily = new FontFamily(fontFamily);
                        if (Math.Abs(textBox.FontSize - 14) < 0.1 || Math.Abs(textBox.FontSize - 16) < 0.1)
                        {
                            textBox.FontSize = fontSize;
                        }
                    }
                    // 更新ComboBox字体
                    else if (element is ComboBox comboBox)
                    {
                        comboBox.FontFamily = new FontFamily(fontFamily);
                        if (Math.Abs(comboBox.FontSize - 14) < 0.1 || Math.Abs(comboBox.FontSize - 16) < 0.1)
                        {
                            comboBox.FontSize = fontSize;
                        }
                    }
                    // 更新Button字体
                    else if (element is Button button)
                    {
                        button.FontFamily = new FontFamily(fontFamily);
                        if (Math.Abs(button.FontSize - 14) < 0.1 || Math.Abs(button.FontSize - 16) < 0.1)
                        {
                            button.FontSize = fontSize;
                        }
                    }
                    // 更新Label字体
                    else if (element is Label label)
                    {
                        label.FontFamily = new FontFamily(fontFamily);
                        if (Math.Abs(label.FontSize - 14) < 0.1 || Math.Abs(label.FontSize - 16) < 0.1)
                        {
                            label.FontSize = fontSize;
                        }
                    }
                }

                // 递归处理子元素
                UpdateControlFonts(child, fontFamily, fontSize);
            }
        }

        private void LoadModelParametersFromConfig()
        {
            // 检查ConfigService是否已初始化
            if (App.ConfigService?.ModelsConfig == null)
                return;

            // 加载默认模型参数
            var modelsConfig = App.ConfigService.ModelsConfig;

            try
            {
                TemperatureSlider.Value = modelsConfig.DefaultTemperature;
                TemperatureValueText.Text = modelsConfig.DefaultTemperature.ToString("F1");

                TopPSlider.Value = modelsConfig.DefaultTopP;
                TopPValueText.Text = modelsConfig.DefaultTopP.ToString("F2");

                TopKSlider.Value = modelsConfig.DefaultTopK;
                TopKValueText.Text = modelsConfig.DefaultTopK.ToString();

                // 如果有当前选中的模型，加载模型特定参数
                if (ModelComboBox.SelectedItem is Model selectedModel)
                {
                    LoadModelSpecificParameters(selectedModel.Name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载默认模型参数失败: {ex.Message}");
            }
        }

        private void LoadModelSpecificParameters(string modelName)
        {
            // 检查ConfigService是否已初始化
            if (App.ConfigService?.ModelsConfig == null)
                return;

            var modelsConfig = App.ConfigService.ModelsConfig;

            try
            {
                if (modelsConfig.ModelSpecificParameters.TryGetValue(modelName, out var modelParams))
                {
                    if (modelParams.Temperature.HasValue)
                    {
                        TemperatureSlider.Value = modelParams.Temperature.Value;
                        TemperatureValueText.Text = modelParams.Temperature.Value.ToString("F1");
                    }

                    if (modelParams.TopP.HasValue)
                    {
                        TopPSlider.Value = modelParams.TopP.Value;
                        TopPValueText.Text = modelParams.TopP.Value.ToString("F2");
                    }

                    if (modelParams.TopK.HasValue)
                    {
                        TopKSlider.Value = modelParams.TopK.Value;
                        TopKValueText.Text = modelParams.TopK.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载模型特定参数失败: {ex.Message}");
            }
        }

        private async void ParameterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 检查ConfigService是否已初始化
            if (App.ConfigService == null || App.ConfigService.ModelsConfig == null)
                return;

            // 更新显示值
            if (sender == TemperatureSlider)
            {
                TemperatureValueText.Text = e.NewValue.ToString("F1");
            }
            else if (sender == TopPSlider)
            {
                TopPValueText.Text = e.NewValue.ToString("F2");
            }
            else if (sender == TopKSlider)
            {
                TopKValueText.Text = e.NewValue.ToString();
            }

            // 保存参数
            await SaveParameterFromSlider(sender, e.NewValue);
        }

        private async void ParameterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 检查ConfigService是否已初始化
            if (App.ConfigService == null || App.ConfigService.ModelsConfig == null)
                return;

            try
            {
                if (sender == TemperatureValueText)
                {
                    if (double.TryParse(TemperatureValueText.Text, out double tempValue))
                    {
                        // 确保值在有效范围内
                        tempValue = Math.Max(0, Math.Min(2, tempValue));

                        // 更新滑块值（但不触发ValueChange事件）
                        TemperatureSlider.ValueChanged -= ParameterSlider_ValueChanged;
                        TemperatureSlider.Value = tempValue;
                        TemperatureSlider.ValueChanged += ParameterSlider_ValueChanged;

                        // 保存参数
                        await SaveParameterFromSlider(TemperatureSlider, tempValue);
                    }
                }
                else if (sender == TopPValueText)
                {
                    if (double.TryParse(TopPValueText.Text, out double topPValue))
                    {
                        // 确保值在有效范围内
                        topPValue = Math.Max(0, Math.Min(1, topPValue));

                        // 更新滑块值（但不触发ValueChange事件）
                        TopPSlider.ValueChanged -= ParameterSlider_ValueChanged;
                        TopPSlider.Value = topPValue;
                        TopPSlider.ValueChanged += ParameterSlider_ValueChanged;

                        // 保存参数
                        await SaveParameterFromSlider(TopPSlider, topPValue);
                    }
                }
                else if (sender == TopKValueText)
                {
                    if (int.TryParse(TopKValueText.Text, out int topKValue))
                    {
                        // 确保值在有效范围内
                        topKValue = Math.Max(1, Math.Min(100, topKValue));

                        // 更新滑块值（但不触发ValueChange事件）
                        TopKSlider.ValueChanged -= ParameterSlider_ValueChanged;
                        TopKSlider.Value = topKValue;
                        TopKSlider.ValueChanged += ParameterSlider_ValueChanged;

                        // 保存参数
                        await SaveParameterFromSlider(TopKSlider, topKValue);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"文本框值变化处理失败: {ex.Message}");
            }
        }

        private async Task SaveParameterFromSlider(object sender, double value)
        {
            // 获取当前选中的模型
            if (ModelComboBox.SelectedItem is Model selectedModel)
            {
                var modelsConfig = App.ConfigService.ModelsConfig;

                // 确保模型参数存在
                if (!modelsConfig.ModelSpecificParameters.ContainsKey(selectedModel.Name))
                {
                    modelsConfig.ModelSpecificParameters[selectedModel.Name] = new ModelParameters();
                }

                var modelParams = modelsConfig.ModelSpecificParameters[selectedModel.Name];

                // 更新参数值
                if (sender == TemperatureSlider)
                {
                    modelParams.Temperature = value;
                }
                else if (sender == TopPSlider)
                {
                    modelParams.TopP = value;
                }
                else if (sender == TopKSlider)
                {
                    modelParams.TopK = (int)value;
                }

                // 保存配置
                try
                {
                    await App.ConfigService.SaveModelsConfigAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存模型配置失败: {ex.Message}");
                }
            }
            else
            {
                // 如果没有选中模型，更新默认参数
                var modelsConfig = App.ConfigService.ModelsConfig;

                if (sender == TemperatureSlider)
                {
                    modelsConfig.DefaultTemperature = value;
                }
                else if (sender == TopPSlider)
                {
                    modelsConfig.DefaultTopP = value;
                }
                else if (sender == TopKSlider)
                {
                    modelsConfig.DefaultTopK = (int)value;
                }

                // 保存配置
                try
                {
                    await App.ConfigService.SaveModelsConfigAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存默认模型配置失败: {ex.Message}");
                }
            }
        }



        private async Task LoadModelsAsync()
        {
            try
            {
                var models = await _ollama.ListLocalModelsAsync();
                ModelComboBox.ItemsSource = models;
                ModelComboBox.DisplayMemberPath = "Name";

                if (models.Any())
                {
                    // 尝试选择配置中的默认模型
                    string defaultModel = App.ConfigService.Config.DefaultModel;
                    if (!string.IsNullOrEmpty(defaultModel))
                    {
                        var defaultModelObj = models.FirstOrDefault(m => m.Name == defaultModel);
                        if (defaultModelObj != null)
                        {
                            ModelComboBox.SelectedItem = defaultModelObj;
                        }
                        else
                        {
                            ModelComboBox.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        ModelComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载模型失败: " + ex.Message);
            }
        }

        private async Task LoadChatHistoryAsync()
        {
            try
            {
                var sessions = await _chatHistoryService.GetSessionsAsync();

                _chatSessions.Clear();
                foreach (var session in sessions)
                {
                    _chatSessions.Add(session);
                }

                // 绑定会话列表到UI
                ChatHistoryListBox.ItemsSource = _chatSessions;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载会话历史失败: " + ex.Message);
            }
        }

        private async Task CreateNewSessionAsync(string title = "新会话")
        {
            try
            {
                var sessionId = await _chatHistoryService.CreateSessionAsync(title);
                _currentSessionId = sessionId;

                // 清空当前聊天消息
                _chatMessages.Clear();

                // 重新加载会话列表
                await LoadChatHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("创建新会话失败: " + ex.Message);
            }
        }

        private async Task LoadSessionAsync(int sessionId)
        {
            try
            {
                _currentSessionId = sessionId;

                // 清空当前聊天消息
                _chatMessages.Clear();

                // 加载会话消息
                var messages = await _chatHistoryService.GetMessagesAsync(sessionId);
                foreach (var message in messages)
                {
                    _chatMessages.Add(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载会话失败: " + ex.Message);
            }
        }

        private async Task SaveMessageAsync(string content, bool isFromUser)
        {
            try
            {
                // 如果是新会话，先创建会话
                if (_currentSessionId == -1)
                {
                    // 使用用户第一条消息的前20个字符作为会话标题
                    var title = isFromUser && content.Length > 20 ? 
                        content.Substring(0, 20) + "..." : 
                        "新会话";

                    await CreateNewSessionAsync(title);
                }

                // 保存消息
                await _chatHistoryService.SaveMessageAsync(_currentSessionId, content, isFromUser);

                // 重新加载会话列表以更新时间
                await LoadChatHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存消息失败: " + ex.Message);
            }
        }

        private async Task DeleteSessionAsync(int sessionId)
        {
            try
            {
                await _chatHistoryService.DeleteSessionAsync(sessionId);

                // 如果删除的是当前会话，创建新会话
                if (sessionId == _currentSessionId)
                {
                    _currentSessionId = -1;
                    _chatMessages.Clear();
                }

                // 重新加载会话列表
                await LoadChatHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("删除会话失败: " + ex.Message);
            }
        }

        private async void ModelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ModelComboBox.SelectedItem is Model selectedModel)
            {
                _ollama.SelectedModel = selectedModel.Name;
                CurrentModelTextBlock.Text = $" - {selectedModel.Name}";

                // 保存为默认模型
                if (App.ConfigService?.Config != null)
                {
                    App.ConfigService.Config.DefaultModel = selectedModel.Name;
                    try
                    {
                        await App.ConfigService.SaveConfigAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"保存默认模型失败: {ex.Message}");
                    }
                }

                // 加载模型特定参数
                if (App.ConfigService?.ModelsConfig != null)
                {
                    LoadModelSpecificParameters(selectedModel.Name);
                }

                // 切换模型时重新创建会话（新上下文）
                _currentChat = new Chat(_ollama);
            }
        }

        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter 键用于换行
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // 手动插入换行符
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    int caretIndex = textBox.CaretIndex;
                    textBox.Text = textBox.Text.Insert(caretIndex, Environment.NewLine);
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;
                }
                // 阻止默认行为
                e.Handled = true;
            }
            // Enter 键用于发送消息
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                // 阻止默认行为（插入换行）
                e.Handled = true;
                // 调用发送方法
                SendMessage();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            string userText = UserInput.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // 添加用户消息
            _chatMessages.Add(new ChatMessage
            {
                Content = userText,
                Alignment = HorizontalAlignment.Right,
                BubbleBackground = new LinearGradientBrush(Color.FromRgb(255, 255, 255), Color.FromRgb(255, 255, 255), 45),
                ShadowColor = Colors.RoyalBlue
            });

            UserInput.Clear();

            // 创建AI消息占位（用于实时追加）
            var aiMessage = new ChatMessage
            {
                Content = "",
                Alignment = HorizontalAlignment.Left,
                BubbleBackground = new LinearGradientBrush(Color.FromRgb(212, 207, 200), Color.FromRgb(212, 207, 200), 90),
                ShadowColor = Colors.Gray
            };
            _chatMessages.Add(aiMessage);

            try
            {
                // 确保有选中的模型
                if (string.IsNullOrEmpty(_ollama.SelectedModel))
                {
                    aiMessage.Content = "错误: 未选择AI模型";
                    return;
                }

                string fullResponse = "";
                
                // 使用流式API获取响应
                _currentChat = new Chat(_ollama);
                _currentChat.Model = _ollama.SelectedModel;
                
                var request = new GenerateRequest { Model = _ollama.SelectedModel, Prompt = userText, Stream = true };
                await foreach (var response in _ollama.GenerateAsync(request))
                {
                    string token = response?.Response ?? "";
                    fullResponse += token;

                    // 输出日志到控制台
                    System.Diagnostics.Debug.WriteLine($"AI回复片段: '{token}'");

                    // 在UI线程上更新内容
                    Dispatcher.Invoke(() => {
                        aiMessage.Content = fullResponse + "▌";  // 生成中闪烁光标
                        ChatScrollViewer.ScrollToEnd();
                    });

                    await Task.Delay(10);  // 增加延迟确保UI更新
                }

                // 输出完整回复到控制台
                System.Diagnostics.Debug.WriteLine($"AI完整回复: '{fullResponse}'");

                aiMessage.Content = fullResponse;  // 生成完成移除光标

                // 保存用户消息和AI回复到数据库
                await SaveMessageAsync(userText, true);
                await SaveMessageAsync(fullResponse, false);
            }
            catch (Exception ex)
            {
                aiMessage.Content = "错误: " + ex.Message;
            }

            ChatScrollViewer.ScrollToEnd();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void ChatHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatHistoryListBox.SelectedItem is ChatSession selectedSession)
            {
                await LoadSessionAsync(selectedSession.Id);
            }
        }

        private async void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int sessionId)
            {
                // 确认删除
                var result = MessageBox.Show(
                    "确定要删除这个会话吗？此操作不可撤销。",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await DeleteSessionAsync(sessionId);
                }
            }
        }

        private async void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            // 清空当前聊天消息
            _chatMessages.Clear();

            // 设置为新会话
            _currentSessionId = -1;

            // 清除会话列表中的选择
            ChatHistoryListBox.SelectedItem = null;

            // 聚焦到输入框
            UserInput.Focus();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = (this.WindowState == WindowState.Maximized)
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ModelManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var modelManagerWindow = new Views.ModelManagerWindow(_ollama);
            modelManagerWindow.Owner = this;
            modelManagerWindow.Show();
        }

        private void SystemSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Views.SystemSettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.Show();
        }

        private bool _isSideBarCollapsed = false;
        private bool _isConfigPanelCollapsed = false;

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isSideBarCollapsed = !_isSideBarCollapsed;
            
            if (_isSideBarCollapsed)
            {
                // 播放折叠动画
                var collapseStoryboard = (Storyboard)FindResource("CollapseSideBarStoryboard");
                var flipArrowStoryboard = (Storyboard)FindResource("FlipArrowStoryboard");
                collapseStoryboard.Begin();
                flipArrowStoryboard.Begin();
            }
            else
            {
                // 播放展开动画
                var expandStoryboard = (Storyboard)FindResource("ExpandSideBarStoryboard");
                var flipArrowBackStoryboard = (Storyboard)FindResource("FlipArrowBackStoryboard");
                expandStoryboard.Begin();
                flipArrowBackStoryboard.Begin();
            }
        }

        private void ConfigToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isConfigPanelCollapsed = !_isConfigPanelCollapsed;

            if (_isConfigPanelCollapsed)
            {
                // 播放折叠动画
                var collapseStoryboard = (Storyboard)FindResource("CollapseConfigPanelStoryboard");
                var flipArrowStoryboard = (Storyboard)FindResource("FlipConfigArrowStoryboard");
                collapseStoryboard.Begin();
                flipArrowStoryboard.Begin();
            }
            else
            {
                // 播放展开动画
                var expandStoryboard = (Storyboard)FindResource("ExpandConfigPanelStoryboard");
                var flipArrowBackStoryboard = (Storyboard)FindResource("FlipConfigArrowBackStoryboard");
                expandStoryboard.Begin();
                flipArrowBackStoryboard.Begin();
            }
        }



        private async Task LoadSystemPromptsAsync()
        {
            try
            {
                var prompts = await _systemPromptService.GetSystemPromptsAsync();

                _systemPrompts.Clear();
                foreach (var prompt in prompts)
                {
                    _systemPrompts.Add(prompt);
                }

                // 设置当前系统提示词
                _currentSystemPrompt = await _systemPromptService.GetActiveSystemPromptAsync();

                // 更新UI
                UpdateSystemPromptList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载系统提示词失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSystemPromptList()
        {
            // 清空现有列表项
            SystemPromptListBox.Items.Clear();

            // 添加系统提示词到列表
            foreach (var prompt in _systemPrompts)
            {
                var item = new ListBoxItem { Content = prompt.Name };

                // 如果是当前使用的系统提示词，设置特殊颜色
                if (_currentSystemPrompt != null && prompt.Id == _currentSystemPrompt.Id)
                {
                    item.Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241)); // #6366F1
                    item.FontWeight = FontWeights.SemiBold;
                }

                SystemPromptListBox.Items.Add(item);
            }
        }

        private async void AddSystemPromptButton_Click(object sender, RoutedEventArgs e)
        {
            var addPromptWindow = new AddSystemPromptWindow(_systemPromptService);
            if (addPromptWindow.ShowDialog() == true)
            {
                // 重新加载系统提示词列表
                await LoadSystemPromptsAsync();
            }
        }

        private async void SystemPromptItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.Content is string promptName)
            {
                // 查找对应的系统提示词
                var prompt = _systemPrompts.FirstOrDefault(p => p.Name == promptName);
                if (prompt != null)
                {
                    // 设置为当前使用的系统提示词
                    await _systemPromptService.SetActiveSystemPromptAsync(prompt.Id);
                    _currentSystemPrompt = prompt;

                    // 更新UI
                    UpdateSystemPromptList();

                    // 如果当前有聊天，可以在这里更新系统提示词
                    // 例如：_currentChat.SetSystemPrompt(prompt.Content);
                }
            }
        }
    }

    // 你的 ChatMessage 类保持不变
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content = "";
        public string Content 
        { 
            get => _content;
            set 
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
        public Brush BubbleBackground { get; set; } = Brushes.White;
        public Color ShadowColor { get; set; } = Colors.White;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}