using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using ProxyChecker.Services;
using Velopack;
using Ursa.Controls;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class UpdateViewModel : ObservableObject, IDialogContext
{
    private readonly UpdateService _updateService;
    private UpdateInfo? _updateInfo;

    [ObservableProperty] private string _title = "检查更新...";
    [ObservableProperty] private string _releaseNotes = "正在获取更新信息...";
    [ObservableProperty] private bool _isChecking = true;
    [ObservableProperty] private bool _isAvailable = false;
    [ObservableProperty] private bool _isDownloading = false;
    [ObservableProperty] private double _progress = 0;
    [ObservableProperty] private string _actionButtonText = "立即更新";
    [ObservableProperty] private bool _canUpdate = false;
    [ObservableProperty] private bool _isReadyToRestart = false;

    // 派生属性：是否显示"稍后"按钮
    public bool ShowLaterButton => !IsReadyToRestart;

    partial void OnIsReadyToRestartChanged(bool value) =>
        OnPropertyChanged(nameof(ShowLaterButton));

#if !DEBUG
    public UpdateViewModel()
    {
        
    }

#endif

    public UpdateViewModel(UpdateService updateService)
    {
        _updateService = updateService;
        _ = CheckUpdatesAsync();
    }

    private async Task CheckUpdatesAsync()
    {
        IsChecking = true;
        Title = "正在检查更新...";

        _updateInfo = await _updateService.CheckForUpdatesAsync();

        IsChecking = false;

        if (_updateInfo != null)
        {
            IsAvailable = true;
            CanUpdate = true;
            Title = $"发现新版本 v{_updateInfo.TargetFullRelease.Version}";
            // Explicitly convert HTML to Markdown or just display as is if supported, 
            // Velopack usually provides markdown or HTML. For now assuming simple text/markdown.
            // _updateInfo.TargetFullRelease.Notes usually empty for GitHub source if not parsed, 
            // but we can try to use Name or Body if available. 
            // Velopack's UpdateInfo might not have Body directly populated from GitHub API in all versions 
            // without custom extension, but let's assume it works or generic text.
            ReleaseNotes = "新版本已发布，建议立即更新以获得最佳体验。";
        }
        else
        {
            Title = "当前已是最新版本";
            ReleaseNotes = "暂无可用更新。";
            CanUpdate = false;
            ActionButtonText = "关闭";
        }
    }

    [RelayCommand]
    private async Task DoUpdateAsync()
    {
        if (IsReadyToRestart)
        {
            _updateService.ApplyUpdatesAndRestart();
            return;
        }

        if (!IsAvailable)
        {
            // Close dialog
            Close();
            return;
        }

        IsDownloading = true;
        IsAvailable = false;
        CanUpdate = false;
        Title = "正在下载更新...";

        try
        {
            await _updateService.DownloadUpdatesAsync(progress => { Progress = progress; });

            IsDownloading = false;
            IsReadyToRestart = true;
            Title = "更新准备就绪";
            ActionButtonText = "立即重启";
            ReleaseNotes = "更新已下载完成，请重启软件以应用更改。";
            CanUpdate = true;
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            IsAvailable = true;
            CanUpdate = true;
            Title = "下载失败";
            ReleaseNotes = $"下载更新时出错：{ex.Message}";
        }
    }

    [RelayCommand]
    private void Skip()
    {
        if (IsReadyToRestart)
        {
            // 下载完成后点击"稍后"，退出应用以便下次启动使用新版本
            App.SkipExitConfirmation = true;
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        else
        {
            Close();
        }
    }

    public void Close()
    {
        RequestClose?.Invoke(this, true);
    }

    public event EventHandler<object?>? RequestClose;
}