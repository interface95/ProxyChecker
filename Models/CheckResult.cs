using CommunityToolkit.Mvvm.ComponentModel;

namespace ProxyChecker.Models;

public enum CheckState
{
    Pending,
    Success,
    Failed
}

public partial class CheckResult : ObservableObject
{
    public ProxyInfo Proxy { get; }

    [ObservableProperty] private string? _realIp;
    [ObservableProperty] private string _isp = string.Empty;
    [ObservableProperty] private string? _location;
    [ObservableProperty] private bool _success;
    [ObservableProperty] private long _responseTimeMs;
    [ObservableProperty] private CheckState _state = CheckState.Pending;
    [ObservableProperty] private string? _error;

    public CheckResult(ProxyInfo proxy)
    {
        Proxy = proxy;
    }

    public static CheckResult Pending(ProxyInfo proxy) => new(proxy);

    public string StatusDisplay => State switch
    {
        CheckState.Pending => "正在检测",
        CheckState.Success => "检测完成",
        CheckState.Failed => Error ?? "检测失败",
        _ => "-"
    };

    public string ResponseTimeDisplay => State == CheckState.Success ? $"{ResponseTimeMs}ms" : "-";
    public string IspDisplay => State == CheckState.Success ? Isp : "-";
    public string RealIpDisplay => State == CheckState.Success ? RealIp ?? "-" : "-";
    public string LocationDisplay => State == CheckState.Success ? Location ?? "-" : "-";

    partial void OnStateChanged(CheckState value)
    {
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(ResponseTimeDisplay));
        OnPropertyChanged(nameof(IspDisplay));
        OnPropertyChanged(nameof(RealIpDisplay));
        OnPropertyChanged(nameof(LocationDisplay));
    }

    partial void OnResponseTimeMsChanged(long value) =>
        OnPropertyChanged(nameof(ResponseTimeDisplay));

    partial void OnIspChanged(string value) =>
        OnPropertyChanged(nameof(IspDisplay));

    partial void OnRealIpChanged(string? value) =>
        OnPropertyChanged(nameof(RealIpDisplay));

    partial void OnLocationChanged(string? value) =>
        OnPropertyChanged(nameof(LocationDisplay));
}
