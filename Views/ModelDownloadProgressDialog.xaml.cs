using System.ComponentModel;
using System.Windows;

namespace AIChat.Views
{
    /// <summary>
    /// ModelDownloadProgressDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ModelDownloadProgressDialog : Window, INotifyPropertyChanged
    {
        private readonly string _modelName;
        private bool _isCancelled = false;
        private DateTime _startTime;
        private long _previousBytes;
        private DateTime _previousTime;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ModelDownloadProgressDialog(string modelName)
        {
            InitializeComponent();
            _modelName = modelName;
            ModelNameTextBlock.Text = $"正在下载: {modelName}";
            DataContext = this;

            _startTime = DateTime.Now;
            _previousTime = DateTime.Now;
        }

        public void UpdateProgress(int progress)
        {
            if (_isCancelled) return;

            Dispatcher.Invoke(() =>
            {
                DownloadProgressBar.Value = progress;
                ProgressTextBlock.Text = $"{progress}%";

                // 模拟下载信息
                var totalSizeMB = 4000; // 假设模型大小为4GB
                var downloadedMB = (int)(totalSizeMB * progress / 100.0);

                DownloadedSizeTextBlock.Text = $"已下载: {downloadedMB} MB";
                TotalSizeTextBlock.Text = $"总大小: {totalSizeMB} MB";

                // 计算下载速度
                var currentTime = DateTime.Now;
                var elapsedSeconds = (currentTime - _previousTime).TotalSeconds;

                if (elapsedSeconds > 0)
                {
                    var previousMB = _previousBytes / (1024.0 * 1024.0);
                    var downloadedDeltaMB = downloadedMB - previousMB;
                    var speedMBps = downloadedDeltaMB / elapsedSeconds;

                    SpeedTextBlock.Text = $"速度: {FormatSpeed((long)(speedMBps * 1024 * 1024))}";

                    // 计算剩余时间
                    if (speedMBps > 0)
                    {
                        var remainingMB = totalSizeMB - downloadedMB;
                        var remainingSeconds = remainingMB / speedMBps;
                        TimeLeftTextBlock.Text = $"剩余时间: {FormatTime(remainingSeconds)}";
                    }
                }

                _previousBytes = (long)(downloadedMB * 1024 * 1024);
                _previousTime = currentTime;

                // 下载完成
                if (progress >= 100)
                {
                    Close();
                }
            });
        }

        private string FormatSpeed(long bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond} B/s";

            if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024.0:F1} KB/s";

            if (bytesPerSecond < 1024 * 1024 * 1024)
                return $"{bytesPerSecond / (1024.0 * 1024):F1} MB/s";

            return $"{bytesPerSecond / (1024.0 * 1024 * 1024):F1} GB/s";
        }

        private string FormatTime(double seconds)
        {
            var time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1)
                return $"{time.Hours}时{time.Minutes}分";

            if (time.TotalMinutes >= 1)
                return $"{time.Minutes}分{time.Seconds}秒";

            return $"{time.Seconds}秒";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            Close();
        }
    }
}
