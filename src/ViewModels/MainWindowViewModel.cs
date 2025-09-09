using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoLaunch;
using AutoLaunchTestTool.Models;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DynamicData.Binding;
using ReactiveUI;

namespace AutoLaunchTestTool.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    #region converters
    /// <summary>
    /// 枚举转值列表
    /// </summary>
    public static readonly IValueConverter EnumToValues = new FuncValueConverter<object, object>(val => val is Type { IsEnum: true } type ? Enum.GetValuesAsUnderlyingType(type).Cast<object>().Select(v => Enum.ToObject(type, v)) : AvaloniaProperty.UnsetValue);
    /// <summary>
    /// bool转green/red
    /// </summary>
    public static readonly IValueConverter BoolToGr = new FuncValueConverter<bool, IBrush>(val => val ? Brushes.Green : Brushes.Red);
    /// <summary>
    /// LogStatus转颜色
    /// </summary>
    public static readonly IValueConverter LogStatusToBrush = new FuncValueConverter<LogStatus, IBrush>(status => status switch
    {
        LogStatus.Success => Brushes.Green,
        LogStatus.Failure => Brushes.Red,
        _ => Brushes.Gray
    });
    #endregion
    
    public AvaloniaList<LogItem> Logs { get; } = [];
    private SafeAutoLauncher? Launcher { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool IsEnable { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool CanSetWorkScope { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool CanSetExtraConfig { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool CanSetIdentifiers { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public bool ShouldRebuild { get; set => this.RaiseAndSetIfChanged(ref field, value); } = true;

    public string AppName { get; set => this.RaiseAndSetIfChanged(ref field, value); } = nameof(AutoLaunchTestTool);
    public string AppPath { get; set => this.RaiseAndSetIfChanged(ref field, value); } = Environment.ProcessPath!;
    public string Args { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "";
    public WorkScope WorkScope { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public WindowsEngine WindowsEngine { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public LinuxEngine LinuxEngine { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public MacOSEngine MacOSEngine { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string Identifiers { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "";
    public string ExtraConfig { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "";

    public ICommand BuildCmd { get; }
    public ICommand EnableCmd { get; }
    public ICommand DisableCmd { get; }
    public ICommand RefreshCmd { get; }
    public ICommand RestartAsAdminCmd { get; }

    public MainWindowViewModel()
    {
        AddLog(LogStatus.Normal, "Launch info", string.Join(" ", Environment.GetCommandLineArgs()));

        IObservable<bool> canExec = this.WhenAnyValue(x => x.Launcher).Select(al => al is not null);
        BuildCmd = ReactiveCommand.CreateFromTask(Build);
        EnableCmd = ReactiveCommand.CreateFromTask(EnableAsync, canExec);
        DisableCmd = ReactiveCommand.CreateFromTask(DisableAsync, canExec);
        RefreshCmd = ReactiveCommand.CreateFromTask(RefreshAsync, canExec);
        RestartAsAdminCmd = ReactiveCommand.Create(RestartAsAdmin);

        // can set WorkScope
        this.WhenAnyValue(x => x.WindowsEngine, x => x.LinuxEngine, x => x.MacOSEngine).Subscribe(_ => { CanSetWorkScope = (OperatingSystem.IsWindows() && WindowsEngine is WindowsEngine.Registry or WindowsEngine.StartupFolder) || (OperatingSystem.IsLinux() && LinuxEngine is LinuxEngine.Freedesktop) || (OperatingSystem.IsMacOS() && MacOSEngine is MacOSEngine.LaunchAgent); });
        // can set ExtraConfig
        this.WhenAnyValue(x => x.MacOSEngine).Subscribe(_ => { CanSetExtraConfig = (OperatingSystem.IsMacOS() && MacOSEngine is MacOSEngine.LaunchAgent) || (OperatingSystem.IsLinux() && LinuxEngine is LinuxEngine.Freedesktop); });
        // can set Identifiers
        this.WhenAnyValue(x => x.MacOSEngine).Subscribe(_ => { CanSetIdentifiers = OperatingSystem.IsMacOS() && MacOSEngine is MacOSEngine.LaunchAgent; });
        // should rebuild
        this.WhenAnyPropertyChanged(nameof(AppName), nameof(AppPath), nameof(Args), nameof(WorkScope), nameof(WindowsEngine), nameof(LinuxEngine), nameof(MacOSEngine), nameof(Identifiers), nameof(ExtraConfig)).Subscribe(_ => { ShouldRebuild = true; });
    }

    private async Task Build()
    {
        ShouldRebuild = false;
        Launcher = new AutoLaunchBuilder()
            .SetAppName(AppName)
            .SetAppPath(AppPath)
            .SetArgs(Args.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .SetWorkScope(WorkScope)
            .SetWindowsEngine(WindowsEngine)
            .SetLinuxEngine(LinuxEngine)
            .SetMacOSEngine(MacOSEngine)
            .SetIdentifiers(Identifiers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
            .SetExtraConfig(ExtraConfig)
            .BuildSafe();
        AddLog(LogStatus.Normal, "Build");
        await RefreshAsync();
    }
    private async Task EnableAsync()
    {
        if (Launcher is null) return;
        if (!await Launcher.TryEnableAsync()) AddLog(LogStatus.Failure, "Enable", Launcher?.TakeLastException()?.ToString());
        else
        {
            AddLog(LogStatus.Success, "Enable");
            await RefreshAsync();
        }
    }
    private async Task DisableAsync()
    {
        if (Launcher is null) return;
        if (!await Launcher.TryDisableAsync()) AddLog(LogStatus.Failure, "Disable", Launcher?.TakeLastException()?.ToString());
        else
        {
            AddLog(LogStatus.Success, "Disable");
            await RefreshAsync();
        }
    }
    private async Task RefreshAsync()
    {
        if (Launcher is null) return;
        (bool success, bool enabled) = await Launcher.TryGetStatusAsync();
        IsEnable = enabled;
        if (success) AddLog(LogStatus.Success, "Refresh", $"IsEnable: {enabled}");
        else AddLog(LogStatus.Failure, "Refresh", Launcher?.TakeLastException()?.ToString());
    }

    private void RestartAsAdmin()
    {
        string res = OSHelpers.RunInAdmin(Environment.ProcessPath!, Environment.GetCommandLineArgs().Skip(1));
        if (!string.IsNullOrEmpty(res)) AddLog(LogStatus.Failure, "Restart as Admin", res);
        else Environment.Exit(0);
    }

    private void AddLog(LogStatus status, string title, string? message = null)
    {
        Logs.Add(new LogItem(status, title, message));
        if (Logs.Count > 100) Logs.RemoveAt(0);
    }
}
