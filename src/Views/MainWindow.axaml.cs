using System.Collections.Specialized;
using AutoLaunchTestTool.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace AutoLaunchTestTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowRoot.LayoutTransform = new ScaleTransform(.8, .8);

        // Auto-scroll log to bottom
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
