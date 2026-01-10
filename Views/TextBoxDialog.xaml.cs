using System.Windows;

namespace HexaFlow.Views
{
    /// <summary>
    /// 用于获取用户输入的简单对话框
    /// </summary>
    public partial class TextBoxDialog : Window
    {
        public string Answer { get; private set; }

        public TextBoxDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            
            Title = title;
            PromptTextBlock.Text = prompt;
            AnswerTextBox.Text = defaultValue;
            AnswerTextBox.Focus();
            AnswerTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Answer = AnswerTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}