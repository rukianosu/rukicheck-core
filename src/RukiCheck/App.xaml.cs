using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RukiCheck.Application;
using RukiCheck.Infrastructure;
using RukiCheck.Services;
using RukiCheck.ViewModels;

namespace RukiCheck;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    public static ServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 依存性注入の設定
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<FileSystemService>();
        services.AddSingleton<JsonReportWriter>();
        services.AddSingleton<HtmlReportGenerator>();

        // Application
        services.AddSingleton<InspectionOrchestrator>();

        // Services（各検査サービス）
        services.AddTransient<StorageInspectionService>();
        services.AddTransient<KeyboardInspectionService>();
        services.AddTransient<MicrophoneInspectionService>();
        services.AddTransient<SpeakerInspectionService>();
        services.AddTransient<CameraInspectionService>();
        services.AddTransient<TrackpadInspectionService>();
        services.AddTransient<CpuInspectionService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<KeyboardTestViewModel>();
        services.AddTransient<TrackpadTestViewModel>();

        _serviceProvider = services.BuildServiceProvider();
        ServiceProvider = _serviceProvider;

        // MainWindowを表示
        var mainWindow = new Views.MainWindow
        {
            DataContext = _serviceProvider.GetService<MainViewModel>()
        };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
