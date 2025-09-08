using System.Collections.Specialized;
using AutoLaunchTestTool.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AutoLaunchTestTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel) viewModel.Logs.CollectionChanged += OnLogsCollectionChanged;
    }

    private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add) Dispatcher.UIThread.Post(LogScrollViewer.ScrollToEnd);
    }
}
