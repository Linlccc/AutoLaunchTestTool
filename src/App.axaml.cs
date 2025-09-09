using System;
using System.Runtime.InteropServices;
using AutoLaunchTestTool.ViewModels;
using AutoLaunchTestTool.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AutoLaunchTestTool;

public class App : Application
{
    public static string OSInfo => $"{Environment.OSVersion.Platform} - {OSHelpers.CurrentOS()} {Environment.OSVersion.Version} ({RuntimeInformation.OSArchitecture})";
    public static string OSDescription => RuntimeInformation.OSDescription;
    public static string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsLinux => OperatingSystem.IsLinux();
    public static bool IsMacOS => OperatingSystem.IsMacOS();
    public static bool IsAdmin => OSHelpers.IsAdmin();
    public static bool IsNotAdmin => !OSHelpers.IsAdmin();
    public static string IsAdminText => OSHelpers.IsAdmin() ? "Yes" : "No";

    public static string RunTime { get; } = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss}";
    public static string Args { get; private set; } = string.Empty;


    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Args = string.Join(" ", desktop.Args ?? []);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
