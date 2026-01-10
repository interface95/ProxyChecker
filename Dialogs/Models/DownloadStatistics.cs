using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ProxyChecker.Dialogs.Models;

public partial class DownloadStatistics : ObservableObject
{
    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private double _speed;

    [ObservableProperty]
    private TimeSpan _remaining;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalBytesToReceiveInMB))]
    private long _totalBytesToReceive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BytesReceivedInMB))]
    private long _bytesReceived;

    [ObservableProperty]
    private double _progressPercentage;

    public double BytesReceivedInMB => (double)BytesReceived / 1024 / 1024;
    public double TotalBytesToReceiveInMB => (double)TotalBytesToReceive / 1024 / 1024;
}
