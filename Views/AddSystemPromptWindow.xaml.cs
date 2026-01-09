using System;
using System.ComponentModel;
using System.Windows;
using HexaFlow.Services;

namespace HexaFlow.Views
{
    public partial class AddSystemPromptWindow : Window, INotifyPropertyChanged
    {
        private readonly SystemPromptService _systemPromptService;
        private string _promptName = string.Empty;
        private string _promptContent = string.Empty;

        public string PromptName
        {
            get => _promptName;
            set
            {
                _promptName = value;
                OnPropertyChanged(nameof(PromptName));
            }
        }

        public string PromptContent
        {
            get => _promptContent;
            set
            {
                _promptContent = value;
                OnPropertyChanged(nameof(PromptContent));
            }
        }

        public AddSystemPromptWindow(SystemPromptService systemPromptService)
        {
            InitializeComponent();
            DataContext = this;
            _systemPromptService = systemPromptService;
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PromptName))
            {
                MessageBox.Show("请输入系统提示词名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                PromptNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptContent))
            {
                MessageBox.Show("请输入系统提示词内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                PromptContentTextBox.Focus();
                return;
            }

            try
            {
                await _systemPromptService.AddSystemPromptAsync(PromptName, PromptContent);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加系统提示词失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
