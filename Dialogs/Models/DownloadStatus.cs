using System.ComponentModel;

namespace ProxyChecker.Dialogs.Models;

public enum DownloadStatus
{
    [Description("未开始")]
    NotStarted,
    [Description("下载中")]
    Downloading,
    [Description("已暂停")]
    Paused,
    [Description("已完成")]
    Completed,
    [Description("失败")]
    Failed
}
