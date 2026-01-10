using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyChecker.Services;
using System.Threading.Tasks;
using Ursa.Controls;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private readonly UpdateService _updateService;

    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _description;

    public AboutViewModel(UpdateService updateService)
    {
        _updateService = updateService;

        // 使用 VersionInfo 获取 Git 标签版本
        AppVersion = VersionInfo.GetDisplayVersion();

        Description = "ProxyChecker 是一个高性能的代理检测工具，支持多协议检测、地理位置识别及并发验证。\n\n基于 Avalonia UI 与 Native AOT 技术构建，旨在提供跨平台、极致流畅的用户体验。";
    }

    // Design-time constructor
    public AboutViewModel()
    {
        _appVersion = VersionInfo.GetDisplayVersion();
        _description = "开发预览版说明文本...";
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        // Check update logic
        // We can close the About dialog and trigger the main update flow, 
        // or trigger it directly. 
        // For simplicity, we'll let the MainViewModel handle the UI transition, but here we can just invoke the service check
        // Ideally, we reuse the existing CheckUpdate logic.
        // Let's assume we want to open the UpdateDialog.

        // Since we are already in a Dialog, we might want to close this one first or show another on top.
        // Ursa supports stacked dialogs.

        var updateInfo = await _updateService.CheckForUpdatesAsync();

        if (updateInfo != null)
        {
            var vm = new UpdateViewModel(_updateService);
            await OverlayDialog.ShowModal<ProxyChecker.Dialogs.Views.UpdateDialog, UpdateViewModel>(
               vm,
               options: new OverlayDialogOptions
               {
                   Title = "发现新版本",
                   CanLightDismiss = false
               });
        }
        else
        {
            await MessageBox.ShowOverlayAsync("当前已是最新版本。", "检查更新");
        }
    }
}
