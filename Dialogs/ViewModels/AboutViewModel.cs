using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyChecker.Dialogs.Views;
using ProxyChecker.Services;
using System;
using System.Threading.Tasks;
using Ursa.Controls;

namespace ProxyChecker.Dialogs.ViewModels;

public partial class AboutViewModel(UpdateService updateService) : ObservableObject
{
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
    private async Task OnCheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        try
        {
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                var vm = new UpdateViewModel(updateService, updateInfo);
                await OverlayDialog.ShowModal<UpdateDialog, UpdateViewModel>(
                    vm,
                    options: new OverlayDialogOptions
                    {
                        Buttons = DialogButton.None,
                        Title = "软件更新",
                        CanLightDismiss = false
                    });
            }
            else
            {
                await MessageBox.ShowOverlayAsync("当前已是最新版本。", "检查更新");
            }
        }
        catch (Exception ex)
        {
            await MessageBox.ShowOverlayAsync($"检查更新失败: {ex.Message}", "错误");
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }
}
