using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using ProxyChecker.Common;

namespace ProxyChecker.Dialogs.Models;

public partial class SettingModel : ObservableObject, IDialogContext
{
    // 导入设置
    [ObservableProperty] private string _separator = ",";
    [ObservableProperty] private int _ipIndex;
    [ObservableProperty] private int _portIndex = 1;
    [ObservableProperty] private int _usernameIndex = 2;
    [ObservableProperty] private int _passwordIndex = 3;

    // 代理设置
    [ObservableProperty] private int _proxyType; // 0=HTTP, 1=SOCKS4, 2=SOCKS5
    [ObservableProperty] private int _timeout = 10; // 默认10秒
    [ObservableProperty] private int _concurrency = 50; // 默认50线程
    [ObservableProperty] private bool _autoSave = true; // 默认自动保存

    public void Close()
    {
        GlobalSetting.Instance.Save();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<object?>? RequestClose;
}
