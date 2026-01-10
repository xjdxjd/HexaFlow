using System;
using System.Collections.Generic;
using System.Windows;

namespace HexaFlow.Views
{
    public partial class ModelManagerDialog : Window
    {
        public ModelManagerDialog()
        {
            InitializeComponent();
        }
    }

    // 模型数据类
    public class ModelInfo
    {
        public string Name { get; set; }
        public string Size { get; set; }
        // TODO: 添加更多属性
    }
}