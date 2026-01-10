using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyChecker.Services;
using System.Threading.Tasks;
using Ursa.Controls;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class AboutViewModel(UpdateService updateService) : ObservableObject
{
    private readonly UpdateService _updateService = updateService;

    [ObservableProperty] private string _appVersion = VersionInfo.GetDisplayVersion();
    [ObservableProperty] private string _description =
        "ProxyChecker 是一个高性能的代理检测工具，支持多协议检测、地理位置识别及并发验证。\n\n基于 Avalonia UI 与 Native AOT 技术构建，旨在提供跨平台、极致流畅的用户体验。";
    [ObservableProperty] private bool _isCheckingUpdate;

    // Design-time constructor
    public AboutViewModel() : this(new UpdateService())
    {
        Description = "开发预览版说明文本...";
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        var updateInfo = await _updateService.CheckForUpdatesAsync();
        IsCheckingUpdate = false;

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
