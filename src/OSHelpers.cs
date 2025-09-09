using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace AutoLaunchTestTool;

public static partial class OSHelpers
{
    public static string CurrentOS()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsLinux()) return "Linux";
        return OperatingSystem.IsMacOS() ? "macOS" : "Unknown OS";
    }

    public static bool IsAdmin()
    {
        if (OperatingSystem.IsWindows()) return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) return geteuid() == 0;
        return false;
    }

    public static string RunInAdmin(string file, IEnumerable<string> args)
    {
        if (OperatingSystem.IsWindows()) return WinRunInAdmin(file, args);
        if (OperatingSystem.IsLinux()) return LinuxRunInSudo(file, args);
        return OperatingSystem.IsMacOS() ? MacRunInSudo(file, args) : "Unsupported OS";
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

    [SupportedOSPlatform("windows")]
    public static string WinRunInAdmin(string file, IEnumerable<string> args)
    {
        Process.Start(new ProcessStartInfo(file, args)
        {
            UseShellExecute = true,
            Verb = "runas"
        });
        return string.Empty;
    }

    [SupportedOSPlatform("linux")]
    public static string LinuxRunInSudo(string file, IEnumerable<string> args)
    {
        if (File.Exists("/usr/bin/pkexec"))
        {
            string arg = $"pkexec env DISPLAY={Environment.GetEnvironmentVariable("DISPLAY")} XAUTHORITY={Environment.GetEnvironmentVariable("XAUTHORITY")} {file} {string.Join(" ", args)} &";
            // Here, you need to use "bash" to execute background(&) commands to prevent the child process from being killed after the parent process exits.
            Process.Start(new ProcessStartInfo("bash", ["-c", arg]) { UseShellExecute = false });
            return string.Empty;
        }
        if (File.Exists("/usr/bin/gnome-terminal"))
        {
            Process.Start(new ProcessStartInfo("gnome-terminal", ["--", "sudo", file, ..args]) { UseShellExecute = false });
            return string.Empty;
        }

        return "No suitable privilege escalation method found, please install pkexec or gnome-terminal.";
    }

    [SupportedOSPlatform("macos")]
    public static string MacRunInSudo(string file, IEnumerable<string> args)
    {
        if (IsRunningInLaunchd()) return "Cannot restart as admin when running in LaunchAgent, please run the app manually.";
        string script = $"""do shell script "{file} {string.Join(" ", args)}" with administrator privileges""";
        Process.Start(new ProcessStartInfo("osascript", ["-e", script]) { UseShellExecute = false });
        return string.Empty;
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
}
