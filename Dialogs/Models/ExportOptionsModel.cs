using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;

using Ursa.Common;
using Ursa.Controls;

namespace ProxyChecker.Dialogs.Models;

public partial class ExportOptionsModel : ObservableObject, IDialogContext
{
    [ObservableProperty] private bool _includeIp = true;
    [ObservableProperty] private bool _includePort = true;
    [ObservableProperty] private bool _includeUsername = true;
    [ObservableProperty] private bool _includePassword = true;
    [ObservableProperty] private bool _includeRealIp = false;
    [ObservableProperty] private bool _includeLocation = false;
    [ObservableProperty] private bool _includeIsp = false;
    [ObservableProperty] private bool _includeResponseTime = false;
    [ObservableProperty] private bool _includeOriginalLine = false;
    [ObservableProperty] private string _separator = ",";
    [ObservableProperty] private bool _onlySuccess = true;

    public void Close()
    {
        Cancel();
    }

    public event EventHandler<object?>? RequestClose;

    public void Confirm() => RequestClose?.Invoke(this, DialogResult.OK);
    public void Cancel() => RequestClose?.Invoke(this, DialogResult.Cancel);

    // 全选所有字段
    public void SelectAll()
    {
        IncludeIp = true;
        IncludePort = true;
        IncludeUsername = true;
        IncludePassword = true;
        IncludeRealIp = true;
        IncludeLocation = true;
        IncludeIsp = true;
        IncludeResponseTime = true;
        IncludeOriginalLine = true;
    }

    // 取消全选
    public void DeselectAll()
    {
        IncludeIp = false;
        IncludePort = false;
        IncludeUsername = false;
        IncludePassword = false;
        IncludeRealIp = false;
        IncludeLocation = false;
        IncludeIsp = false;
        IncludeResponseTime = false;
        IncludeOriginalLine = false;
    }
}
