using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ProxyChecker.ViewModels;
using Ursa.Controls;

namespace ProxyChecker.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        // 设置拖放事件
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;

        var files = e.Data.GetFiles();
        if (files == null) return;

        foreach (var file in files)
        {
            if (file is IStorageFile storageFile)
            {
                var path = storageFile.Path.LocalPath;
                if (path.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    await viewModel.LoadFileFromPathAsync(path);
                    break;
                }
            }
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainViewModel vm)
        {
            // 订阅集合变化事件
            vm.Results.CollectionChanged -= OnResultsCollectionChanged;
            vm.Results.CollectionChanged += OnResultsCollectionChanged;
        }
    }

    private void OnResultsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
        {
            // 自动滚动到最后一行
            // 使用 Dispatcher 确保 UI 更新后执行滚动
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ResultsGrid.Rows != null && ResultsGrid.Rows.Count > 0 && ResultsGrid.Scroll != null)
                {
                    ResultsGrid.Scroll.Offset = new Avalonia.Vector(0, ResultsGrid.Scroll.Extent.Height);
                }
            });
        }
    }
}
