using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using ProxyChecker.Dialogs.Models;
using ProxyChecker.Services;
using Ursa.Controls;
using Velopack;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class UpdateViewModel : ObservableObject, IDialogContext
{
    public static bool IsUpdateDialogOpen { get; set; }

    private readonly UpdateService _updateService;
    private UpdateInfo? _updateInfo;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private DownloadStatistics _statistics = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand))]
    private DownloadStatus _status = DownloadStatus.NotStarted;

    [ObservableProperty] private string? _releaseNotes;

    // Design-time constructor
    public UpdateViewModel()
    {
        _updateService = null!;
    }

    // Runtime constructor
    public UpdateViewModel(UpdateService updateService, UpdateInfo? updateInfo = null)
    {
        _updateService = updateService;
        _updateInfo = updateInfo;

        if (_updateInfo != null)
        {
            Statistics.Version = _updateInfo.TargetFullRelease.Version.ToString();
            var notes = _updateInfo.TargetFullRelease.NotesMarkdown;
            ReleaseNotes = !string.IsNullOrWhiteSpace(notes) ? notes : "暂无更新日志";
        }
        else
        {
            _ = CheckUpdatesAsync();
        }
    }

    private async Task CheckUpdatesAsync()
    {
        try
        {
            Status = DownloadStatus.NotStarted;
            _updateInfo = await _updateService.CheckForUpdatesAsync();

            if (_updateInfo != null)
            {
                Statistics.Version = _updateInfo.TargetFullRelease.Version.ToString();
                ReleaseNotes = !string.IsNullOrWhiteSpace(_updateInfo.TargetFullRelease.NotesMarkdown)
                    ? _updateInfo.TargetFullRelease.NotesMarkdown
                    : "暂无更新日志";
            }
            else
            {
                // No update found - dialog will be closed by AboutViewModel
                Status = DownloadStatus.Completed;
            }
        }
        catch (Exception ex)
        {
            Status = DownloadStatus.Failed;
            await MessageBox.ShowOverlayAsync($"检查更新失败: {ex.Message}", "错误", icon: MessageBoxIcon.Error);
        }
    }

    public bool CanStart => Status is DownloadStatus.NotStarted or DownloadStatus.Paused or DownloadStatus.Failed;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (_updateInfo == null) return;

        Status = DownloadStatus.Downloading;
        _cts = new CancellationTokenSource();

        try
        {
            await _updateService.DownloadUpdatesAsync(progress =>
            {
                Statistics.ProgressPercentage = progress;
            }, _cts.Token);

            Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DownloadStatus.Paused;
        }
        catch (Exception ex)
        {
            Status = DownloadStatus.Failed;
            await MessageBox.ShowOverlayAsync($"下载更新失败: {ex.Message}", "错误", icon: MessageBoxIcon.Error);
        }
    }

    public bool CanStop => Status is DownloadStatus.Downloading;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private async Task RestartAsync()
    {
        await StartAsync();
    }

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void ApplyAndRestart()
    {
        _updateService.ApplyUpdatesAndRestart();
    }

    public event EventHandler<object?>? RequestClose;
}
