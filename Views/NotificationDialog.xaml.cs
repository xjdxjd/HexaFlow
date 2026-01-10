using System.Windows;

namespace HexaFlow.Views
{
    /// <summary>
    /// 自定义通知弹窗
    /// </summary>
    public partial class NotificationDialog : Window
    {
        public NotificationDialog(string title, string message)
        {
            InitializeComponent();
            
            Title = title;
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
            
            // 设置窗口大小适应内容
            this.SizeToContent = SizeToContent.Manual;
            this.Width = 400;
            this.Height = 180;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}