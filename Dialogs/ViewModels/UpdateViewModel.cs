using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using ProxyChecker.Dialogs.Models;
using ProxyChecker.Services;
using Velopack;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class UpdateViewModel : ObservableObject, IDialogContext
{
    private readonly UpdateService _updateService;
    private UpdateInfo? _updateInfo;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private DownloadStatistics _statistics = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RestartCommand))]
    private DownloadStatus _status = DownloadStatus.NotStarted;

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
            // 直接开始下载，UpdateService 已在 AboutViewModel 中初始化
            _ = StartAsync();
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
                // Auto start download if update is found, as we are in the UpdateDialog
                await StartAsync();
            }
            else
            {
                // No update found - dialog will be closed by AboutViewModel
                Status = DownloadStatus.Completed;
            }
        }
        catch (Exception)
        {
            Status = DownloadStatus.Failed;
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
            // TODO: 添加日志记录 ex.Message
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
