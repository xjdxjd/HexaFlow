using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HexaFlow.Services;
using HexaFlow.Models;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Media;

namespace HexaFlow.Views
{
    public partial class MainWindow : Window
    {
        private OllamaService _ollamaService;
        private ChatHistoryService _chatHistoryService;
        private string _currentModel = "未选择模型";
        private List<Message> _messages;
        private bool _isGeneratingResponse = false;
        private Conversation _currentConversation;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 初始化服务
            _ollamaService = new OllamaService();
            _chatHistoryService = new ChatHistoryService();
            
            // 初始化当前对话
            _currentConversation = new Conversation();
            
            // 绑定按钮事件
            MinimizeButton.Click += MinimizeButton_Click;
            MaximizeButton.Click += MaximizeButton_Click;
            CloseButton.Click += CloseButton_Click;
            ModelManagerButton.Click += ModelManagerButton_Click;
            
            // 加载模型列表
            _ = LoadModelsAsync();
            
            // 检查并更新Ollama连接状态
            _ = UpdateConnectionStatusAsync();
            
            // 启动连接状态监控定时器
            StartConnectionStatusMonitoring();
            
            // 初始化消息列表
            _messages = new List<Message>();
            
            // 等待界面加载完成后获取UI元素
            Loaded += async (s, e) => {
                var inputTextBox = (TextBox)FindName("InputTextBox");
                var historyListBox = (ListBox)FindName("HistoryListBox");
                
                // 初始化输入框
                inputTextBox.GotFocus += (s2, e2) => {
                    if (inputTextBox.Text == "输入消息...")
                        inputTextBox.Text = "";
                };
                inputTextBox.LostFocus += (s2, e2) => {
                    if (string.IsNullOrWhiteSpace(inputTextBox.Text))
                        inputTextBox.Text = "输入消息...";
                };
                
                // 加载历史对话列表
                await LoadHistoryConversationsAsync();
                
                // 为历史列表添加选择事件
                historyListBox.SelectionChanged += HistoryListBox_SelectionChanged;
                
                // 为新建会话按钮添加点击事件
                var newConversationButton = (Button)FindName("NewConversationButton");
                newConversationButton.Click += (s2, e2) => NewConversation();
            };
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
        
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ModelManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var modelManagerDialog = new ModelManagerDialog();
            modelManagerDialog.Owner = this;
            modelManagerDialog.ShowDialog();
        }

        /// <summary>
        /// 异步加载模型列表
        /// </summary>
        private async Task LoadModelsAsync()
        {
            try
            {
                // 检查控件是否存在
                if (ModelComboBox == null)
                {
                    return;
                }
                
                // 检查Ollama服务是否可用
                bool isAvailable = await _ollamaService.IsAvailableAsync();
                if (!isAvailable)
                {
                    ModelComboBox.Items.Add("Ollama服务不可用");
                    return;
                }

                // 获取本地模型列表
                var models = await _ollamaService.GetLocalModelsAsync();
                
                // 清空现有项目
                ModelComboBox.Items.Clear();
                
                // 添加模型到下拉列表
                foreach (var model in models)
                {
                    ModelComboBox.Items.Add(model.Name);
                }
                
                // 如果有模型，默认选择第一个
                if (models.Count > 0)
                {
                    ModelComboBox.SelectedIndex = 0;
                    _currentModel = models[0].Name;
                    UpdateCurrentModelDisplay();
                }
            }
            catch (Exception ex)
            {
                if (ModelComboBox != null)
                {
                    ModelComboBox.Items.Add($"加载模型失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 更新当前模型显示
        /// </summary>
        private void UpdateCurrentModelDisplay()
        {
            if (CurrentModelButton != null)
            {
                CurrentModelButton.Content = $"当前模型: {_currentModel}";
            }
        }

        /// <summary>
        /// 双击模型选择框事件
        /// </summary>
        private void ModelComboBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ModelComboBox != null && ModelComboBox.SelectedItem != null)
            {
                _currentModel = ModelComboBox.SelectedItem.ToString() ?? "";
                UpdateCurrentModelDisplay();
            }
        }

        /// <summary>
        /// 更新Ollama连接状态显示
        /// </summary>
        private async Task UpdateConnectionStatusAsync()
        {
            try
            {
                bool isConnected = await _ollamaService.IsAvailableAsync();
                
                Dispatcher.Invoke(() =>
                {
                    if (isConnected)
                    {
                        ConnectionStatusTextBlock.Text = "● 已连接";
                        ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
                    }
                    else
                    {
                        ConnectionStatusTextBlock.Text = "● 未连接";
                        ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
                    }
                });
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() =>
                {
                    ConnectionStatusTextBlock.Text = "● 连接错误";
                    ConnectionStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
                });
            }
        }

        /// <summary>
        /// 开始连接状态监控
        /// </summary>
        private void StartConnectionStatusMonitoring()
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5); // 每5秒检查一次
            timer.Tick += async (sender, e) =>
            {
                await UpdateConnectionStatusAsync();
            };
            timer.Start();
        }
        
        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
        
        /// <summary>
        /// 输入框按键事件
        /// </summary>
        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // Ctrl+Enter 发送消息
                    SendMessage();
                    e.Handled = true;
                }
                // Enter键仅换行，不发送消息
                else if (Keyboard.Modifiers == ModifierKeys.None)
                {
                    // 允许换行
                }
            }
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        private async void SendMessage()
        {
            var inputTextBox = (TextBox)FindName("InputTextBox");
            string message = inputTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(message) || message == "输入消息...")
            {
                return; // 不发送空消息
            }
            
            // 添加用户消息到聊天记录
            var userMessage = new Message("user", message);
            _messages.Add(userMessage);
            AddMessageToUI(userMessage);
            
            // 如果是新对话，自动生成标题
            if (_currentConversation.Id == 0 && _messages.Count == 1)
            {
                _currentConversation.Title = _chatHistoryService.GenerateTitleFromFirstMessage(message);
            }
            
            // 清空输入框
            inputTextBox.Text = "";
            
            // 滚动到底部
            await ScrollToBottomAsync();
            
            // 添加助手消息占位符，显示加载动画
            var assistantMessage = new Message("assistant", "");
            _messages.Add(assistantMessage);
            var assistantElement = AddMessageToUI(assistantMessage);
            
            // 开始生成助手回复
            await GenerateAssistantResponse(assistantMessage, assistantElement);
            
            // 更新对话时间并保存
            _currentConversation.UpdatedAt = DateTime.Now;
            _currentConversation.Messages = _messages.ToList(); // 保存消息副本
            await _chatHistoryService.SaveConversationAsync(_currentConversation);
        }
        
        /// <summary>
        /// 将消息添加到UI
        /// </summary>
        private Border AddMessageToUI(Message message)
        {
            var messageBubble = new Border
            {
                Margin = new Thickness(0, 5, 0, 5),
                Padding = new Thickness(16, 12, 16, 12),
                CornerRadius = new CornerRadius(18, 18, message.Role == "user" ? 6 : 18, message.Role == "user" ? 18 : 6),
                HorizontalAlignment = message.Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Background = message.Role == "user" ? 
                    (Brush)FindResource("UserMessageBackgroundBrush") : 
                    (Brush)FindResource("AssistantMessageBackgroundBrush")
            };

            var grid = new Grid();
            messageBubble.Child = grid;

            var row1 = new RowDefinition { Height = GridLength.Auto };
            var row2 = new RowDefinition { Height = GridLength.Auto };
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);

            // 时间戳
            var timeTextBlock = new TextBlock
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 10,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                HorizontalAlignment = message.Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            Grid.SetRow(timeTextBlock, 0);
            grid.Children.Add(timeTextBlock);

            // 消息内容
            var contentTextBlock = new TextBlock
            {
                Name = "MessageContent",
                Text = message.Content,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = message.Role == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            Grid.SetRow(contentTextBlock, 1);
            grid.Children.Add(contentTextBlock);

            // 添加到消息面板
            var messagesPanel = (StackPanel)FindName("MessagesPanel");
            messagesPanel.Children.Add(messageBubble);

            return messageBubble;
        }
        
        /// <summary>
        /// 更新消息UI内容
        /// </summary>
        private void UpdateMessageUI(Border messageBubble, string newContent)
        {
            var grid = (Grid)messageBubble.Child;
            var contentTextBlock = (TextBlock)grid.Children[1]; // 第二个子元素是内容TextBlock
            contentTextBlock.Text = newContent;
        }
        
        /// <summary>
        /// 生成助手回复（模拟流式显示效果）
        /// </summary>
        private async Task GenerateAssistantResponse(Message assistantMessage, Border assistantElement)
        {
            if (_isGeneratingResponse) return;
            
            _isGeneratingResponse = true;
            
            try
            {
                // 准备历史消息
                var history = new List<MessageContent>();
                foreach (var msg in _messages)
                {
                    if (msg != assistantMessage) // 不包含当前助手消息占位符
                    {
                        history.Add(new MessageContent { Role = msg.Role, Content = msg.Content });
                    }
                }
                
                // 获取完整响应
                var fullResponse = await _ollamaService.ChatAsync(
                    _messages[_messages.Count - 2].Content, // 最后一条用户消息
                    _currentModel,
                    history);
                
                // 模拟流式显示效果
                var currentText = "";
                foreach (char c in fullResponse)
                {
                    currentText += c;
                    // 在UI线程上更新响应
                    Dispatcher.Invoke(() =>
                    {
                        assistantMessage.Content = currentText;
                        UpdateMessageUI(assistantElement, currentText);
                        
                        // 滚动到底部
                        _ = ScrollToBottomAsync(); // 修复警告
                    });
                    
                    // 延迟以产生流式效果
                    await Task.Delay(30); // 30ms延迟
                }
            }
            catch (Exception ex)
            {
                // 添加错误消息
                _messages.RemoveAt(_messages.Count - 1); // 移除占位符
                var errorElement = (StackPanel)FindName("MessagesPanel");
                errorElement.Children.RemoveAt(errorElement.Children.Count - 1); // 移除UI上的占位符
                
                var errorMessage = new Message("assistant", $"错误: {ex.Message}");
                _messages.Add(errorMessage);
                AddMessageToUI(errorMessage);
            }
            finally
            {
                // 更新对话时间并保存
                _currentConversation.UpdatedAt = DateTime.Now;
                _currentConversation.Messages = _messages.ToList(); // 保存消息副本
                await _chatHistoryService.SaveConversationAsync(_currentConversation);
                
                _isGeneratingResponse = false;
            }
        }
        
        /// <summary>
        /// 滚动到聊天底部
        /// </summary>
        private async Task ScrollToBottomAsync()
        {
            // 延迟执行以确保UI已更新
            await Task.Delay(10);
            
            Dispatcher.Invoke(() => {
                var chatScrollViewer = (ScrollViewer)FindName("ChatScrollViewer");
                chatScrollViewer?.ScrollToBottom();
            });
        }
        
        /// <summary>
        /// 加载历史对话列表
        /// </summary>
        private async Task LoadHistoryConversationsAsync()
        {
            try
            {
                var historyListBox = (ListBox)FindName("HistoryListBox");
                var conversations = await _chatHistoryService.GetAllConversationsAsync();
                
                historyListBox.ItemsSource = conversations;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史对话失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 历史对话选择事件
        /// </summary>
        private async void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selectedConversation = (Conversation)e.AddedItems[0];
                if (selectedConversation != null)
                {
                    // 加载选中的对话
                    var conversation = await _chatHistoryService.GetConversationByIdAsync(selectedConversation.Id);
                    
                    if (conversation != null)
                    {
                        // 更新当前对话
                        _currentConversation = conversation;
                        _messages = conversation.Messages.ToList();
                        
                        // 清空当前消息显示
                        var messagesPanel = (StackPanel)FindName("MessagesPanel");
                        messagesPanel.Children.Clear();
                        
                        // 重新显示消息
                        foreach (var message in _messages)
                        {
                            AddMessageToUI(message);
                        }
                        
                        // 滚动到底部
                        await ScrollToBottomAsync();
                    }
                }
            }
        }
        
        /// <summary>
        /// 创建新对话
        /// </summary>
        private void NewConversation()
        {
            _currentConversation = new Conversation();
            _messages.Clear();
            
            // 清空当前消息显示
            var messagesPanel = (StackPanel)FindName("MessagesPanel");
            messagesPanel.Children.Clear();
            
            // 重新加载历史对话列表
            _ = LoadHistoryConversationsAsync();
        }
    }
}