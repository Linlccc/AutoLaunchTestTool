using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoLaunch;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DynamicData.Binding;
using ReactiveUI;

namespace AutoLaunchTestTool.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    /// <summary>
    /// 枚举转值列表
    /// </summary>
    public static readonly IValueConverter EnumToValues = new FuncValueConverter<object, object>(val => val is Type { IsEnum: true } type ? Enum.GetValuesAsUnderlyingType(type).Cast<object>().Select(v => Enum.ToObject(type, v)) : AvaloniaProperty.UnsetValue);
    /// <summary>
    /// bool转green/red
    /// </summary>
    public static readonly IValueConverter BoolToGr = new FuncValueConverter<bool, IBrush>(val => val ? Brushes.Green : Brushes.Red);
    public static readonly IValueConverter BoolToRt = new FuncValueConverter<bool, IBrush>(val => val ? Brushes.Red : Brushes.Transparent);

    public string OSInfo => $"{Environment.OSVersion.Platform} - {OSHelpers.CurrentOS()} {Environment.OSVersion.Version} ({RuntimeInformation.OSArchitecture})";
    public string OSDescription => RuntimeInformation.OSDescription;
    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;

    public bool IsAdmin => OSHelpers.IsAdministrator();
    public bool IsWindows => OperatingSystem.IsWindows();
    public bool IsLinux => OperatingSystem.IsLinux();
    public bool IsMacOS => OperatingSystem.IsMacOS();


    private SafeAutoLauncher? Launcher { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string Message { get; set => this.RaiseAndSetIfChanged(ref field, value); }
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
        Message = $"launch info: {string.Join(" ", Environment.GetCommandLineArgs())}";
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
        await RefreshAsync();
    }
    private async Task EnableAsync()
    {
        if (Launcher is null) return;
        if (await Launcher.TryEnableAsync()) await RefreshAsync();
        else ShowError("Enable");
    }
    private async Task DisableAsync()
    {
        if (Launcher is null) return;
        if (await Launcher.TryDisableAsync()) await RefreshAsync();
        else ShowError("Disable");
    }
    private async Task RefreshAsync()
    {
        if (Launcher is null) return;
        (bool success, bool enabled) = await Launcher.TryIsEnabledAsync();
        IsEnable = enabled;
        if (success) Message = $"{DateTime.Now:hh:mm:ss.f}: \t{enabled}";
        else ShowError("Refresh");
    }
    private void ShowError(string action) => Message = $"{action} failed:{Environment.NewLine}{Launcher?.TakeLastException()}";

    private void RestartAsAdmin()
    {
        if (IsAdmin) return;

        string fileName = Environment.ProcessPath!;
        IEnumerable<string> args = Environment.GetCommandLineArgs().Skip(1);
        try
        {
            ProcessStartInfo proc;
            if (IsWindows)
            {
                proc = new(fileName, args);
                proc.WorkingDirectory = Environment.CurrentDirectory;
                proc.UseShellExecute = true;
                proc.Verb = "runas";
            }
            else if (IsLinux)
            {
                // linux 中重启有问题
                if (File.Exists("/usr/bin/pkexec")) proc = new("pkexec", [fileName, ..args]);
                else if (File.Exists("/usr/bin/gnome-terminal")) proc = new("gnome-terminal", ["--", "sudo", fileName, ..args]);
                else throw new Exception("No suitable privilege escalation method found, please install pkexec or gnome-terminal.");
                proc.UseShellExecute = false;
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (OSHelpers.IsRunningInLaunchd())
                {
                    Message = "Cannot restart as admin when running in LaunchAgent, please run the app manually.";
                    return;
                }
                string script = $"""do shell script "{fileName} {string.Join(" ", args)}" with administrator privileges""";
                proc = new("osascript", ["-e", script]);
                proc.UseShellExecute = false;
            }
            else throw new PlatformNotSupportedException("Only Windows, Linux and macOS are supported.");

            Process.Start(proc);
            Environment.Exit(0);
        }
        catch (Exception e)
        {
            Message = $"Restart as admin failed: {e}";
        }
    }
}

public static partial class OSHelpers
{
    [UnsupportedOSPlatform("windows")]
    [LibraryImport("libc")]
    private static partial uint geteuid();

    [UnsupportedOSPlatform("windows")]
    [LibraryImport("libc")]
    private static partial int getppid();

    [SupportedOSPlatform("macos")]
    [LibraryImport("libproc")]
    private static partial int proc_name(int pid, IntPtr buffer, uint buffersize);

    [SupportedOSPlatform("macos")]
    [LibraryImport("libproc")]
    private static partial int proc_pidpath(int pid, IntPtr buffer, uint buffersize);

    [SupportedOSPlatform("macos")]
    public static string ParentProcessName()
    {
        IntPtr buffer = Marshal.AllocHGlobal(1024);
        _ = proc_name(getppid(), buffer, 1024);
        string? parentName = Marshal.PtrToStringAnsi(buffer);
        Marshal.FreeHGlobal(buffer);
        return parentName ?? "unknown";
    }

    [SupportedOSPlatform("macos")]
    public static bool IsRunningInLaunchd()
    {
        int ppid = getppid();
        if (ppid == 1) return true;

        IntPtr buffer = Marshal.AllocHGlobal(4096);
        try
        {
            if (proc_pidpath(ppid, buffer, 4096) <= 0) return false;
            string parentPath = Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
            return parentPath.EndsWith("/launchd", StringComparison.Ordinal);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static string CurrentOS()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        return OperatingSystem.IsMacOS() ? "macOS" : "Unknown OS";
    }

    public static bool IsAdministrator()
    {
        if (OperatingSystem.IsWindows()) return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) return geteuid() == 0;
        return false;
    }
}
