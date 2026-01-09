using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace HexaFlow.Models
{
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
