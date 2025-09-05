using System;
using AutoLaunchTestTool.ViewModels;
using AutoLaunchTestTool.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AutoLaunchTestTool;

public class App : Application
{
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
