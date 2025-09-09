using System;

namespace AutoLaunchTestTool.Models;

public class LogItem(LogStatus status, string title, string? message)
{
    public DateTime DateTime { get; set; } = DateTime.Now;

    public LogStatus Status { get; set; } = status;

    public string Title { get; set; } = title;

    public string? Message { get; set; } = message;

    public string DisplayText => $"[{DateTime:HH:mm:ss}] {Title}";
}

public enum LogStatus
{
    Normal,
    Success,
    Failure
}
