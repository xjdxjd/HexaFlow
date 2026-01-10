using System;
using System.Windows;
using HexaFlow.Utils;

namespace HexaFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 检查系统字体
        var systemFonts = FontHelper.GetSystemFonts();
        bool hasHarmonyFont = FontHelper.FontExists("HarmonyOS Sans SC");
        
        if (hasHarmonyFont)
        {
            // 如果找到HarmonyOS Sans SC字体，则更新全局字体设置
            var defaultFont = new System.Windows.Media.FontFamily("HarmonyOS Sans SC");
            var alternativeFont = new System.Windows.Media.FontFamily("HarmonyOS Sans SC");
            
            Resources["DefaultFont"] = defaultFont;
            Resources["AlternativeFont"] = alternativeFont;
        }
    }
}