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
using System.Linq;
using System.Windows.Data;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Windows.Media.Animation;
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
        
        // 用于窗口最大化功能的变量
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;
        private WindowState _originalWindowState;
        private bool _isMaximized = false;
        
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
                // 保存原始窗口状态
                _originalWidth = Width;
                _originalHeight = Height;
                _originalLeft = Left;
                _originalTop = Top;
                _originalWindowState = WindowState;
                
                // 监听窗口状态变化事件
                StateChanged += MainWindow_StateChanged;
                
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
            if (_isMaximized)
            {
                // 恢复到原始大小和位置
                Width = _originalWidth;
                Height = _originalHeight;
                Left = _originalLeft;
                Top = _originalTop;
                _isMaximized = false;
            }
            else
            {
                // 保存当前窗口状态
                _originalWidth = Width;
                _originalHeight = Height;
                _originalLeft = Left;
                _originalTop = Top;
                _originalWindowState = WindowState;
                
                // 获取当前屏幕的工作区
                RECT workArea = GetMonitorWorkArea();
                
                // 设置窗口大小为当前屏幕的工作区大小（不遮挡任务栏）
                WindowState = WindowState.Normal; // 先恢复正常状态
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Right - workArea.Left;
                Height = workArea.Bottom - workArea.Top;
                _isMaximized = true;
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            // 当窗口通过其他方式（如拖拽边缘或双击标题栏）最大化时，调整为当前屏幕的工作区大小
            if (WindowState == WindowState.Maximized && !_isMaximized)
            {
                // 获取当前屏幕的工作区
                RECT workArea = GetMonitorWorkArea();
                
                // 立即调整为当前屏幕的工作区大小（不遮挡任务栏）
                WindowState = WindowState.Normal; // 先恢复正常状态
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Right - workArea.Left;
                Height = workArea.Bottom - workArea.Top;
                _isMaximized = true;
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
        
        private RECT GetMonitorWorkArea()
        {
            IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
            
            GetMonitorInfo(hMonitor, ref monitorInfo);
            
            return monitorInfo.rcWork;
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
        
        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelComboBox != null && ModelComboBox.SelectedItem != null)
            {
                _currentModel = ModelComboBox.SelectedItem.ToString() ?? "";
                UpdateCurrentModelDisplay();
            }
        }
        
        private void ModelComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ModelComboBox != null)
            {
                // 切换下拉列表的展开状态
                ModelComboBox.IsDropDownOpen = !ModelComboBox.IsDropDownOpen;
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
            var sendButton = (Button)FindName("SendButton");
            string message = inputTextBox.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(message) || message == "输入消息...")
            {
                return; // 不发送空消息
            }
            
            // 禁用发送按钮，更改图标为刷新图标并开始旋转
            if (sendButton != null)
            {
                sendButton.Content = "\uE72C"; // 刷新图标
                sendButton.IsEnabled = false;
                
                // 添加旋转动画
                var rotateTransform = new RotateTransform();
                sendButton.RenderTransform = rotateTransform;
                sendButton.RenderTransformOrigin = new Point(0.5, 0.5);
                
                var animation = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(2));
                animation.RepeatBehavior = RepeatBehavior.Forever;
                
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
            
            try
            {
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
            finally
            {
                // 恢复发送按钮
                if (sendButton != null)
                {
                    // 停止旋转动画
                    sendButton.ClearValue(Button.RenderTransformProperty);
                    
                    sendButton.Content = "\uF0AD"; // 向上箭头图标
                    sendButton.IsEnabled = true;
                }
            }
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

            // 添加右键菜单
            var contextMenu = new ContextMenu();
            contextMenu.Style = (Style)FindResource("JadeContextMenuStyle");
            
            // 为用户消息添加编辑选项
            if (message.Role == "user")
            {
                var editMenuItem = new MenuItem { Header = "编辑消息" };
                editMenuItem.Click += EditUserMessage_Click;
                editMenuItem.DataContext = message;
                editMenuItem.Style = (Style)FindResource("JadeMenuItemStyle");
                contextMenu.Items.Add(editMenuItem);
                
                var separator = new Separator();
                separator.Style = (Style)FindResource("JadeSeparatorStyle");
                contextMenu.Items.Add(separator);
            }
            
            // 添加复制选项
            var copyMenuItem = new MenuItem { Header = "复制消息" };
            copyMenuItem.Click += CopyMessage_Click;
            copyMenuItem.DataContext = message;
            copyMenuItem.Style = (Style)FindResource("JadeMenuItemStyle");
            contextMenu.Items.Add(copyMenuItem);
            
            var regenerateMenuItem = new MenuItem { Header = "重新生成回复" };
            regenerateMenuItem.Click += RegenerateResponse_Click;
            regenerateMenuItem.DataContext = message;
            regenerateMenuItem.Style = (Style)FindResource("JadeMenuItemStyle");
            contextMenu.Items.Add(regenerateMenuItem);
            
            messageBubble.ContextMenu = contextMenu;

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
        
        #region 操作便捷功能
        
        /// <summary>
        /// 复制单条消息
        /// </summary>
        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.DataContext is Message message)
            {
                Clipboard.SetText(message.Content);
                ShowNotification("消息已复制到剪贴板");
            }
        }
        
        /// <summary>
        /// 复制全部对话
        /// </summary>
        private void CopyAllMessages_Click(object sender, RoutedEventArgs e)
        {
            var allContent = string.Join("\n\n", _messages.Select(msg => 
                $"[{msg.Role.ToUpper()}]: {msg.Content}"));
            Clipboard.SetText(allContent);
            ShowNotification("全部对话已复制到剪贴板");
        }
        
        /// <summary>
        /// 重新生成当前回答
        /// </summary>
        private async void RegenerateResponse_Click(object sender, RoutedEventArgs e)
        {
            if (_isGeneratingResponse) return;
            
            // 找到最后一个用户消息和对应的助手消息
            var lastUserMessageIndex = -1;
            var lastAssistantMessageIndex = -1;
            
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                if (_messages[i].Role == "user")
                {
                    lastUserMessageIndex = i;
                    break;
                }
            }
            
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                if (_messages[i].Role == "assistant")
                {
                    lastAssistantMessageIndex = i;
                    break;
                }
            }
            
            if (lastUserMessageIndex != -1)
            {
                // 如果存在之前的助手回复，移除它
                if (lastAssistantMessageIndex != -1 && lastAssistantMessageIndex > lastUserMessageIndex)
                {
                    // 从UI中移除助手消息
                    var messagesPanel = (StackPanel)FindName("MessagesPanel");
                    if (messagesPanel.Children.Count > 0)
                    {
                        messagesPanel.Children.RemoveAt(messagesPanel.Children.Count - 1);
                    }
                    
                    // 从消息列表中移除助手消息
                    _messages.RemoveAt(lastAssistantMessageIndex);
                }
                
                // 添加新的助手消息占位符
                var assistantMessage = new Message("assistant", "");
                _messages.Add(assistantMessage);
                var assistantElement = AddMessageToUI(assistantMessage);
                
                // 生成新的回复
                await GenerateAssistantResponse(assistantMessage, assistantElement);
                
                // 更新对话时间并保存
                _currentConversation.UpdatedAt = DateTime.Now;
                _currentConversation.Messages = _messages.ToList();
                await _chatHistoryService.SaveConversationAsync(_currentConversation);
            }
        }
        
        /// <summary>
        /// 编辑用户消息并重新提交
        /// </summary>
        private void EditUserMessage_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem?.DataContext is Message message && message.Role == "user")
            {
                var index = _messages.IndexOf(message);
                if (index != -1)
                {
                    // 显示编辑对话框
                    var editDialog = new TextBoxDialog("编辑消息", "请输入新的消息内容：", message.Content);
                    editDialog.Owner = this;
                    if (editDialog.ShowDialog() == true)
                    {
                        var newContent = editDialog.Answer;
                        if (!string.IsNullOrWhiteSpace(newContent))
                        {
                            // 更新消息内容
                            _messages[index] = new Message(message.Role, newContent);
                            
                            // 更新UI
                            var messagesPanel = (StackPanel)FindName("MessagesPanel");
                            if (index < messagesPanel.Children.Count)
                            {
                                var messageBubble = (Border)messagesPanel.Children[index];
                                var grid = (Grid)messageBubble.Child;
                                var contentTextBlock = (TextBlock)grid.Children[1];
                                contentTextBlock.Text = newContent;
                            }
                            
                            // 重新生成后续的助手回复
                            ReSubmitFromIndex(index);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 从指定索引重新提交对话
        /// </summary>
        private async void ReSubmitFromIndex(int startIndex)
        {
            // 移除startIndex之后的所有消息
            var messagesToRemove = new List<int>();
            for (int i = _messages.Count - 1; i > startIndex; i--)
            {
                messagesToRemove.Add(i);
            }
            
            // 从后往前移除，避免索引变化
            foreach (var index in messagesToRemove)
            {
                _messages.RemoveAt(index);
            }
            
            // 从UI中移除对应的消息元素
            var messagesPanel = (StackPanel)FindName("MessagesPanel");
            for (int i = messagesPanel.Children.Count - 1; i > startIndex; i--)
            {
                messagesPanel.Children.RemoveAt(i);
            }
            
            // 如果最后一个消息是用户消息，则重新生成助手回复
            if (_messages.Count > 0 && _messages[_messages.Count - 1].Role == "user")
            {
                var assistantMessage = new Message("assistant", "");
                _messages.Add(assistantMessage);
                var assistantElement = AddMessageToUI(assistantMessage);
                
                await GenerateAssistantResponse(assistantMessage, assistantElement);
                
                // 更新对话时间并保存
                _currentConversation.UpdatedAt = DateTime.Now;
                _currentConversation.Messages = _messages.ToList();
                await _chatHistoryService.SaveConversationAsync(_currentConversation);
            }
        }
        
        /// <summary>
        /// 清空当前对话
        /// </summary>
        private void ClearCurrentConversation_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要清空当前对话吗？此操作无法撤销。", "确认清空", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _messages.Clear();
                
                // 清空当前消息显示
                var messagesPanel = (StackPanel)FindName("MessagesPanel");
                messagesPanel.Children.Clear();
                
                // 重置当前对话
                _currentConversation = new Conversation();
                
                ShowNotification("对话已清空");
            }
        }
        
        #endregion
        
        #region 导出功能
        
        /// <summary>
        /// 导出对话
        /// </summary>
        private void ExportConversation_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var exportType = menuItem.Tag?.ToString();
                ExportConversation(exportType);
            }
        }
        
        /// <summary>
        /// 导出对话为指定格式
        /// </summary>
        private void ExportConversation(string format)
        {
            if (_messages == null || !_messages.Any())
            {
                ShowNotification("当前没有对话可导出");
                return;
            }
            
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = GetFilterByFormat(format),
                FileName = _currentConversation.Title != "新对话" ? _currentConversation.Title : "未命名对话",
                DefaultExt = GetExtensionByFormat(format)
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string content = GetExportContent(format);
                    System.IO.File.WriteAllText(saveFileDialog.FileName, content, System.Text.Encoding.UTF8);
                    ShowNotification($"对话已导出为{format?.ToUpper() ?? "未知"}格式");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "导出错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 根据格式获取过滤器
        /// </summary>
        private string GetFilterByFormat(string format)
        {
            switch (format?.ToLower())
            {
                case "markdown":
                    return "Markdown 文件 (*.md)|*.md|所有文件 (*.*)|*.*";
                case "json":
                    return "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                case "txt":
                    return "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                default:
                    return "所有文件 (*.*)|*.*";
            }
        }
        
        /// <summary>
        /// 根据格式获取扩展名
        /// </summary>
        private string GetExtensionByFormat(string format)
        {
            switch (format?.ToLower())
            {
                case "markdown":
                    return ".md";
                case "json":
                    return ".json";
                case "txt":
                    return ".txt";
                default:
                    return ".txt";
            }
        }
        
        /// <summary>
        /// 获取导出内容
        /// </summary>
        private string GetExportContent(string format)
        {
            switch (format?.ToLower())
            {
                case "markdown":
                    return GetMarkdownContent();
                case "json":
                    return GetJsonContent();
                case "txt":
                    return GetTxtContent();
                default:
                    return GetTxtContent(); // 默认返回TXT格式
            }
        }
        
        /// <summary>
        /// 获取Markdown格式内容
        /// </summary>
        private string GetMarkdownContent()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# {_currentConversation.Title ?? "对话记录"}\n");
            sb.AppendLine($"日期: {_currentConversation.CreatedAt:yyyy-MM-dd HH:mm:ss}\n");
            
            foreach (var msg in _messages)
            {
                var role = msg.Role == "user" ? "用户" : "助手";
                var prefix = msg.Role == "user" ? "> [!NOTE]" : "> [!TIP]";
                sb.AppendLine($"{prefix} **{role}**\n> \n> {msg.Content.Replace("\n", "\n> ")}\n\n---\n");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取JSON格式内容
        /// </summary>
        private string GetJsonContent()
        {
            var conversationData = new
            {
                Title = _currentConversation.Title,
                CreatedAt = _currentConversation.CreatedAt,
                UpdatedAt = _currentConversation.UpdatedAt,
                Messages = _messages.Select(m => new { m.Role, m.Content, m.Timestamp }).ToList()
            };
            
            return Newtonsoft.Json.JsonConvert.SerializeObject(conversationData, 
                Newtonsoft.Json.Formatting.Indented);
        }
        
        /// <summary>
        /// 获取TXT格式内容
        /// </summary>
        private string GetTxtContent()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{_currentConversation.Title ?? "对话记录"}");
            sb.AppendLine($"日期: {_currentConversation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 50));
            
            foreach (var msg in _messages)
            {
                var role = msg.Role == "user" ? "用户" : "助手";
                sb.AppendLine($"[{role}] {msg.Timestamp:HH:mm}: {msg.Content}");
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        /// <summary>
        /// 显示通知
        /// </summary>
        private void ShowNotification(string message)
        {
            // 创建临时通知文本块
            var notification = new Border
            {
                Background = (Brush)FindResource("PrimaryButtonBrush"),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(6),
                Visibility = Visibility.Visible
            };
            
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 14,
                Foreground = (Brush)FindResource("AccentTextBrush"),
                TextAlignment = TextAlignment.Center
            };
            
            notification.Child = textBlock;
            
            // 添加到主窗口的Grid中
            var mainGrid = (Grid)FindName("MainGrid");
            if (mainGrid != null)
            {
                mainGrid.Children.Add(notification);
                
                // 使用Dispatcher延时以确保UI已更新
                Dispatcher.BeginInvoke(new Action(() => {
                    // 设置位置
                    Grid.SetRowSpan(notification, 2); // 跨越所有行
                    
                    // 创建定时器，3秒后移除通知
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, e) =>
                    {
                        if (mainGrid.Children.Contains(notification))
                        {
                            mainGrid.Children.Remove(notification);
                        }
                        timer.Stop();
                    };
                    timer.Start();
                }));
            }
        }
    }
}