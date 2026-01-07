using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HexaFlow.Views
{
    // 软件配置数据类
    public class ConfigData
    {
        public string Language { get; set; }
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public string ModelPath { get; set; }
        public string SysPromptsPath { get; set; } = "D:\\HexaFlow\\SysPrompts";
        public bool AutoUpdate { get; set; }
        public bool MinimizeOnStart { get; set; }
    }
    
    // 模型配置数据类
    public class ModelData
    {
        public float Temperature { get; set; } = 0.7f;
        public int Seed { get; set; } = -1;
        public float Top_P { get; set; } = 0.9f;
        public int Top_K { get; set; } = 40;
    }
    /// <summary>
    /// SystemSettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SystemSettingsWindow : Window
    {
        public SystemSettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            
            // 添加字体下拉列表展开事件
            FontFamilyComboBox.DropDownOpened += FontFamilyComboBox_DropDownOpened;

            // 添加自动保存事件处理程序
            ThemeComboBox.SelectionChanged += AutoSaveSettings;
            LanguageComboBox.SelectionChanged += AutoSaveSettings;
            FontFamilyComboBox.SelectionChanged += AutoSaveSettings;
            FontSizeComboBox.SelectionChanged += AutoSaveSettings;
            ModelPathTextBox.TextChanged += AutoSaveSettings;
            SysPromptsPathTextBox.TextChanged += AutoSaveSettings;
            AutoUpdateCheckBox.Checked += AutoSaveSettings;
            AutoUpdateCheckBox.Unchecked += AutoSaveSettings;
            MinimizeOnStartCheckBox.Checked += AutoSaveSettings;
            MinimizeOnStartCheckBox.Unchecked += AutoSaveSettings;
        }
        
        // 字体下拉列表展开事件处理
        private void FontFamilyComboBox_DropDownOpened(object sender, EventArgs e)
        {
            LoadSystemFonts();
        }
        
        // 加载系统字体
        private void LoadSystemFonts()
        {
            // 如果已经加载过系统字体，则不再加载
            // 使用标记来判断是否已加载
            if (FontFamilyComboBox.Tag != null && FontFamilyComboBox.Tag.ToString() == "Loaded") return;
            
            // 标记为已加载
            FontFamilyComboBox.Tag = "Loaded";
            
            // 检查系统中是否有HarmonyOS Sans SC字体
            bool hasHarmonyFont = false;
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                if (fontFamily.Source == "HarmonyOS Sans SC")
                {
                    hasHarmonyFont = true;
                    break;
                }
            }
            
            // 如果没有HarmonyOS Sans SC字体，则设置微软雅黑为默认字体
            if (!hasHarmonyFont)
            {
                // 取消HarmonyOS Sans SC的选中状态
                foreach (ComboBoxItem item in FontFamilyComboBox.Items)
                {
                    if (item.Content.ToString() == "HarmonyOS Sans SC")
                    {
                        item.IsSelected = false;
                    }
                    else if (item.Content.ToString() == "微软雅黑")
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
            }
            
            // 获取系统字体
            var systemFonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
            
            // 保存当前选中的字体
            string selectedFont = null;
            if (FontFamilyComboBox.SelectedItem != null)
            {
                selectedFont = ((ComboBoxItem)FontFamilyComboBox.SelectedItem).Content.ToString();
            }
            
            // 清空现有项（保留前4个默认字体）
            var defaultItems = new object[FontFamilyComboBox.Items.Count];
            FontFamilyComboBox.Items.CopyTo(defaultItems, 0);
            
            // 添加系统字体
            // 创建一个HashSet来存储已存在的字体名称，提高查找效率
            var existingFonts = new HashSet<string>();
            foreach (ComboBoxItem item in FontFamilyComboBox.Items)
            {
                existingFonts.Add(item.Content.ToString());
            }
            
            // 添加系统字体（只添加不存在的）
            foreach (var fontFamily in systemFonts)
            {
                if (!existingFonts.Contains(fontFamily.Source))
                {
                    FontFamilyComboBox.Items.Add(new ComboBoxItem { Content = fontFamily.Source });
                    existingFonts.Add(fontFamily.Source);
                }
            }
            
            // 恢复之前选中的字体
            if (!string.IsNullOrEmpty(selectedFont))
            {
                for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
                {
                    if (((ComboBoxItem)FontFamilyComboBox.Items[i]).Content.ToString() == selectedFont)
                    {
                        FontFamilyComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }
        
        // 保存软件配置
        private void SaveConfig(ConfigData config)
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存软件配置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // 保存模型配置
        private void SaveModel(ModelData model)
        {
            try
            {
                string modelPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.json");
                string json = System.Text.Json.JsonSerializer.Serialize(model, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(modelPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存模型配置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            // 从配置文件加载设置
            try
            {
                // 加载软件配置
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                ConfigData config = null;

                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    config = System.Text.Json.JsonSerializer.Deserialize<ConfigData>(json);
                }
                else
                {
                    // 如果配置文件不存在，创建默认配置
                    config = new ConfigData
                    {
                        Language = "中文",
                        FontFamily = "HarmonyOS Sans SC",
                        FontSize = 16,
                        ModelPath = "D:\\AIChat\\Models",
                        AutoUpdate = true,
                        MinimizeOnStart = false
                    };

                    // 保存默认配置
                    SaveConfig(config);
                }

                // 加载模型配置
                string modelConfigPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.json");
                ModelData modelConfig = null;

                if (System.IO.File.Exists(modelConfigPath))
                {
                    string json = System.IO.File.ReadAllText(modelConfigPath);
                    modelConfig = System.Text.Json.JsonSerializer.Deserialize<ModelData>(json);
                }
                else
                {
                    // 如果模型配置文件不存在，创建默认配置
                    modelConfig = new ModelData();

                    // 保存默认配置
                    SaveModel(modelConfig);
                }

                // 应用软件配置
                if (config != null)
                {
                    string settingsPath =
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");

                    if (System.IO.File.Exists(settingsPath))
                    {
                        string json = System.IO.File.ReadAllText(settingsPath);
                        // 不再需要读取旧的Settings.json文件，直接使用config

                        if (config != null)
                        {
                            // 设置语言
                            for (int i = 0; i < LanguageComboBox.Items.Count; i++)
                            {
                                if (((ComboBoxItem)LanguageComboBox.Items[i]).Content.ToString() == config.Language)
                                {
                                    LanguageComboBox.SelectedIndex = i;
                                    break;
                                }
                            }

                            // 设置字体
                            for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
                            {
                                if (((ComboBoxItem)FontFamilyComboBox.Items[i]).Content.ToString() == config.FontFamily)
                                {
                                    FontFamilyComboBox.SelectedIndex = i;
                                    break;
                                }
                            }

                            // 设置字体大小
                            for (int i = 0; i < FontSizeComboBox.Items.Count; i++)
                            {
                                if (((ComboBoxItem)FontSizeComboBox.Items[i]).Content.ToString() ==
                                    config.FontSize.ToString())
                                {
                                    FontSizeComboBox.SelectedIndex = i;
                                    break;
                                }
                            }

                            ModelPathTextBox.Text = config.ModelPath ?? "D:\\AIChat\\Models";
                            SysPromptsPathTextBox.Text = config.SysPromptsPath ?? "D:\\HexaFlow\\SysPrompts";
                            AutoUpdateCheckBox.IsChecked = config.AutoUpdate;
                            MinimizeOnStartCheckBox.IsChecked = config.MinimizeOnStart;
                            return;
                        }
                    }
                }
            }
            
            catch (Exception ex)
            {
                MessageBox.Show($"加载设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 如果加载失败或文件不存在，使用默认值
            LanguageComboBox.SelectedIndex = 0;
            FontFamilyComboBox.SelectedIndex = 0;
            FontSizeComboBox.SelectedIndex = 1;
            ModelPathTextBox.Text = "D:\\AIChat\\Models";
            SysPromptsPathTextBox.Text = "D:\\HexaFlow\\SysPrompts";
            AutoUpdateCheckBox.IsChecked = true;
            MinimizeOnStartCheckBox.IsChecked = false;
        }

        private void SaveSettings()
        {
            // 保存设置到配置文件
            // 实际应用中应该将设置保存到配置文件
            try
            {
                // 使用JSON序列化保存设置到文件
                var config = new ConfigData
                {
                    Language = ((ComboBoxItem)LanguageComboBox.SelectedItem).Content.ToString(),
                    FontFamily = ((ComboBoxItem)FontFamilyComboBox.SelectedItem).Content.ToString(),
                    FontSize = Convert.ToInt32(((ComboBoxItem)FontSizeComboBox.SelectedItem).Content),
                    ModelPath = ModelPathTextBox.Text,
                    SysPromptsPath = SysPromptsPathTextBox.Text,
                    AutoUpdate = AutoUpdateCheckBox.IsChecked ?? false,
                    MinimizeOnStart = MinimizeOnStartCheckBox.IsChecked ?? false
                };
                
                // 保存ConfigData到config.json
                SaveConfig(config);
                
                // 同时保存到Settings.json以保持兼容性
                string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");
                string json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BrowseModelPathButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用简单的输入框让用户输入路径
            var inputDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择模型保存位置",
                Filter = "文件夹|*.folder",
                FileName = "选择此文件夹",
                InitialDirectory = ModelPathTextBox.Text,
                CheckPathExists = true
            };

            if (inputDialog.ShowDialog() == true)
            {
                // 获取文件夹路径
                string folderPath = System.IO.Path.GetDirectoryName(inputDialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    ModelPathTextBox.Text = folderPath;
                }
            }
        }

        private void BrowseSysPromptsPathButton_Click(object sender, RoutedEventArgs e)
        {
            // 使用简单的输入框让用户输入路径
            var inputDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "选择系统提示词保存位置",
                Filter = "文件夹|*.folder",
                FileName = "选择此文件夹",
                InitialDirectory = SysPromptsPathTextBox.Text,
                CheckPathExists = true
            };

            if (inputDialog.ShowDialog() == true)
            {
                // 获取文件夹路径
                string folderPath = System.IO.Path.GetDirectoryName(inputDialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    SysPromptsPathTextBox.Text = folderPath;
                }
            }
        }

        // 自动保存设置
        private void AutoSaveSettings(object sender, RoutedEventArgs e)
        {
            SaveSettings();

            // 如果是字体设置改变，更新应用程序字体
            if (sender is ComboBox comboBox)
            {
                if (comboBox.Name == "FontFamilyComboBox" || comboBox.Name == "FontSizeComboBox")
                {
                    UpdateApplicationFont();
                }
            }
        }

        // 更新应用程序字体
        private void UpdateApplicationFont()
        {
            try
            {
                // 获取当前选择的字体和大小
                string fontFamily = ((ComboBoxItem)FontFamilyComboBox.SelectedItem)?.Content.ToString() ?? "Microsoft YaHei UI";
                int fontSize = Convert.ToInt32(((ComboBoxItem)FontSizeComboBox.SelectedItem)?.Content ?? "16");

                // 更新配置服务中的字体设置
                if (App.ConfigService?.Config != null)
                {
                    App.ConfigService.Config.FontFamily = fontFamily;
                    App.ConfigService.Config.FontSize = fontSize;
                    App.ConfigService.SaveConfigAsync();
                }

                // 更新主窗口字体
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    // 更新主窗口样式
                    var style = new Style(typeof(Window));
                    style.Setters.Add(new Setter(Window.FontFamilyProperty, new FontFamily(fontFamily)));
                    style.Setters.Add(new Setter(Window.FontSizeProperty, (double)fontSize));
                    mainWindow.Style = style;

                    // 更新主窗口中的特定控件字体
                    UpdateControlFonts(mainWindow, fontFamily, fontSize);
                }

                // 更新系统设置窗口自身字体
                var selfStyle = new Style(typeof(Window));
                selfStyle.Setters.Add(new Setter(Window.FontFamilyProperty, new FontFamily(fontFamily)));
                selfStyle.Setters.Add(new Setter(Window.FontSizeProperty, (double)fontSize));
                this.Style = selfStyle;

                // 更新系统设置窗口中的特定控件字体
                UpdateControlFonts(this, fontFamily, fontSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新字体设置时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已保存", "系统设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置为默认设置
            LanguageComboBox.SelectedIndex = 0;
            FontFamilyComboBox.SelectedIndex = 0;
            FontSizeComboBox.SelectedIndex = 1;
            ModelPathTextBox.Text = "D:\\AIChat\\Models";
            AutoUpdateCheckBox.IsChecked = true;
            MinimizeOnStartCheckBox.IsChecked = false;
            MessageBox.Show("设置已重置为默认值", "系统设置", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 支持拖动窗口
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
            base.OnMouseDown(e);
        }
    }
}
