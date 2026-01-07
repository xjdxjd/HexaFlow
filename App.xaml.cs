using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using HexaFlow.Services;

namespace HexaFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static ConfigService _configService;

    /// <summary>
    /// 获取配置服务实例
    /// </summary>
    public static ConfigService ConfigService => _configService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化配置服务
        _configService = new ConfigService();
        await _configService.LoadConfigurationsAsync();
    }
}