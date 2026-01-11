using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyChecker.Common;
using ProxyChecker.Dialogs.Models;
using ProxyChecker.Dialogs.Views;
using ProxyChecker.Models;
using ProxyChecker.Services;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;
using ProxyChecker.Dialogs.ViewModels;

namespace ProxyChecker.ViewModels;

public enum AppPage { Main, About }

public partial class MainViewModel : ObservableObject
{
    // === 文件 ===
    [ObservableProperty] private string? _loadedFileName;
    [ObservableProperty] private string? _loadedFilePath;
    private List<ProxyInfo> _proxies = [];

    // === 设置 ===
    [ObservableProperty] private int _concurrency;
    [ObservableProperty] private bool _autoSave;

    // === 状态 ===
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage = "";

    // 组合繁忙状态
    public bool IsBusy => IsLoading || IsRunning;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBusy));
        NotifyCanExecuteChanged();
    }

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseButtonText));

    // === 进度 ===
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _completedCount;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _elapsedTime = "00:00:00";
    [ObservableProperty] private string _speed = "0个/秒";

    // === 统计 ===
    [ObservableProperty] private int _successCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private int _uniqueIpCount;
    [ObservableProperty] private int _mobileCount;
    [ObservableProperty] private int _telecomCount;
    [ObservableProperty] private int _unicomCount;
    [ObservableProperty] private int _otherCount;

    // === 结果 ===
    [ObservableProperty] private ObservableCollection<CheckResult> _results = [];
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _filterIsp = "全部";

    // === 页面导航 ===
    [ObservableProperty] private AppPage _currentPage = AppPage.Main;
    [ObservableProperty] private int _pageIndex;

    public bool IsMainViewVisible => CurrentPage == AppPage.Main;
    public bool IsAboutViewVisible => CurrentPage == AppPage.About;

    [ObservableProperty] private AboutViewModel _aboutViewModel;

    partial void OnCurrentPageChanged(AppPage value)
    {
        OnPropertyChanged(nameof(IsMainViewVisible));
        OnPropertyChanged(nameof(IsAboutViewVisible));
    }

    // === TreeDataGrid ===
    [ObservableProperty] private FlatTreeDataGridSource<CheckResult>? _resultsSource;

    // === 控制 ===
    private CancellationTokenSource? _cts;
    private readonly AsyncManualResetEvent _pauseEvent = new(true);
    private readonly HashSet<string> _uniqueIps = [];
    private readonly object _fileLock = new();
    private Stopwatch? _stopwatch;
    private IStorageProvider? _storageProvider;
    private readonly UpdateService _updateService;

    public MainViewModel()
    {
        _updateService = new UpdateService();
        _aboutViewModel = new AboutViewModel(_updateService);

        // 从配置加载设置
        var setting = GlobalSetting.Instance.Setting;
        _concurrency = setting.Concurrency;
        _autoSave = setting.AutoSave;

        UpdateResultsSource();

        // 启动时自动检查更新
        _ = CheckUpdateOnStartupAsync();
    }

    private async Task CheckUpdateOnStartupAsync()
    {
        try
        {
            // 延迟 2 秒，让主界面先完成加载
            await Task.Delay(2000);

            if (UpdateViewModel.IsUpdateDialogOpen) return;

            var updateInfo = await _updateService.CheckForUpdatesAsync();
            if (updateInfo != null)
            {
                var vm = new UpdateViewModel(_updateService, updateInfo);
                await OverlayDialog.ShowModal<UpdateDialog, UpdateViewModel>(
                    vm,
                    options: new OverlayDialogOptions
                    {
                        Buttons = DialogButton.None,
                        Title = "软件更新",
                        CanLightDismiss = false
                    });
            }
        }
        catch
        {
            // 静默失败，不打扰用户
        }
    }

    partial void OnConcurrencyChanged(int value)
    {
        GlobalSetting.Instance.Setting.Concurrency = value;
        GlobalSetting.Instance.Save();
    }

    partial void OnAutoSaveChanged(bool value)
    {
        GlobalSetting.Instance.Setting.AutoSave = value;
        GlobalSetting.Instance.Save();
    }

    public void SetStorageProvider(IStorageProvider provider)
    {
        _storageProvider = provider;
    }

    private IClipboard? _clipboard;
    public void SetClipboard(IClipboard clipboard)
    {
        _clipboard = clipboard;
    }

    // === 计算属性 ===
    public bool CanStart => !IsRunning && _proxies.Count > 0;
    public bool CanPause => IsRunning;
    public bool CanStop => IsRunning;
    public string PauseButtonText => IsPaused ? "继续" : "暂停";
    public bool HasNoData => TotalCount == 0;
    public bool HasData => TotalCount > 0;

    // 进度文本: "18/100 (18.0%)"
    public string ProgressText => TotalCount > 0
        ? $"{CompletedCount}/{TotalCount} ({ProgressPercent:F1}%)"
        : "0/0 (0%)";

    // ISP 分布文本: "移动:5 电信:4 联通:3 其他:2"
    public string IspDistribution => $"移动:{MobileCount} 电信:{TelecomCount} 联通:{UnicomCount} 其他:{OtherCount}";

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasNoData));
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(ProgressText));
    }

    partial void OnCompletedCountChanged(int value) => OnPropertyChanged(nameof(ProgressText));
    partial void OnProgressPercentChanged(double value) => OnPropertyChanged(nameof(ProgressText));
    partial void OnMobileCountChanged(int value) => OnPropertyChanged(nameof(IspDistribution));
    partial void OnTelecomCountChanged(int value) => OnPropertyChanged(nameof(IspDistribution));
    partial void OnUnicomCountChanged(int value) => OnPropertyChanged(nameof(IspDistribution));
    partial void OnOtherCountChanged(int value) => OnPropertyChanged(nameof(IspDistribution));

    private void NotifyCanExecuteChanged()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanStop));
    }

    private void UpdateResultsSource()
    {
        var filtered = FilterResults();
        ResultsSource = new FlatTreeDataGridSource<CheckResult>(filtered)
        {
            Columns =
            {
                new TextColumn<CheckResult, int>("#", x => x.Proxy.Index, new GridLength(50)),
                new TextColumn<CheckResult, string>("代理地址", x => x.Proxy.Address, new GridLength(150)),
                new TextColumn<CheckResult, string>("实际IP", x => x.RealIpDisplay, new GridLength(140)),
                new TextColumn<CheckResult, string>("用户名", x => x.Proxy.Username, new GridLength(100)),
                new TextColumn<CheckResult, string>("密码", x => x.Proxy.Password, new GridLength(100)),
                new TextColumn<CheckResult, string>("归属地", x => x.LocationDisplay, new GridLength(180)),
                new TextColumn<CheckResult, string>("ISP", x => x.IspDisplay, new GridLength(100)),
                new TextColumn<CheckResult, string>("响应时间", x => x.ResponseTimeDisplay, new GridLength(100)),
                new TextColumn<CheckResult, string>("状态", x => x.StatusDisplay, new GridLength(1, GridUnitType.Star)),
            }
        };
    }

    private IEnumerable<CheckResult> FilterResults()
    {
        var query = Results.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            query = query.Where(r =>
                r.Proxy.Address.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (r.RealIp?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                r.Proxy.Username.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (FilterIsp != "全部")
        {
            query = FilterIsp == "失败"
                ? query.Where(r => r.State == CheckState.Failed)
                : query.Where(r => r.Success && IspIdentifier.GetIspGroup(r.Isp) == FilterIsp);
        }

        return query;
    }

    partial void OnSearchTextChanged(string value) => UpdateResultsSource();
    partial void OnFilterIspChanged(string value) => UpdateResultsSource();
    partial void OnResultsChanged(ObservableCollection<CheckResult> value) => UpdateResultsSource();

    [RelayCommand]
    private void ShowAbout()
    {
        CurrentPage = AppPage.About;
        PageIndex = 1;
    }

    [RelayCommand]
    private void GoBack()
    {
        CurrentPage = AppPage.Main;
        PageIndex = 0;
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            var options = new DrawerOptions
            {
                Buttons = DialogButton.None,
                Position = Position.Right,
                CanLightDismiss = true,
                IsCloseButtonVisible = true,
                Title = "设置",
            };

            await Drawer.ShowModal<SettingDialog, SettingModel>(
                GlobalSetting.Instance.Setting, null, options);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowOverlayAsync($"打开设置失败: {ex.Message}", "错误");
        }

    }

    [RelayCommand]
    private async Task LoadFileAsync()
    {
        try
        {
            if (_storageProvider == null) return;

            var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择代理文件",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("代理文件") { Patterns = ["*.txt"] }]
            });

            if (files.Count == 0) return;

            var file = files[0];
            await LoadFileFromPathAsync(file.Path.LocalPath);
        }
        catch (Exception ex)
        {
            await MessageBox.ShowOverlayAsync($"打开设置失败: {ex.Message}", "错误");
        }

    }

    public async Task LoadFileFromPathAsync(string filePath)
    {
        try
        {
            if (IsRunning)
            {
                var result = await MessageBox.ShowOverlayAsync(
                    "当前正在进行检测任务，导入新文件将停止当前任务。\n是否继续？",
                    "提示",
                    icon: MessageBoxIcon.Warning,
                    button: MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes) return;

                _cts?.Cancel();
                _pauseEvent.Set();
                IsRunning = false;
                IsPaused = false;
            }

            // 新导入前清空全部数据
            ClearAll();

            IsLoading = true;
            LoadingMessage = "正在加载文件...";

            _proxies = await Task.Run(() => ProxyFileParser.Parse(filePath));
            LoadedFileName = Path.GetFileName(filePath);
            LoadedFilePath = Path.GetDirectoryName(filePath);
            TotalCount = _proxies.Count;
            NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            LoadedFileName = $"解析失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = "";
        }
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        if (_proxies.Count == 0) return;

        ResetRunState();
        var ct = StartRun();
        var service = new ProxyCheckerService(GlobalSetting.Instance.Setting.Timeout);

        try
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Concurrency,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(_proxies, parallelOptions, async (proxy, token) =>
            {
                await ProcessProxyAsync(service, proxy, token);
            });
        }
        catch (OperationCanceledException)
        {
            // 用户停止
        }
        finally
        {
            FinishRun();
        }
    }

    private void ResetRunState()
    {
        IsRunning = true;
        IsPaused = false;
        CompletedCount = 0;
        SuccessCount = 0;
        FailedCount = 0;
        UniqueIpCount = 0;
        MobileCount = 0;
        TelecomCount = 0;
        UnicomCount = 0;
        OtherCount = 0;
        _uniqueIps.Clear();
        Results.Clear();
        ProgressPercent = 0;
    }

    private CancellationToken StartRun()
    {
        _cts = new CancellationTokenSource();
        _pauseEvent.Set();
        _stopwatch = Stopwatch.StartNew();

        if (AutoSave)
        {
            ClearOutputFiles();
        }

        _ = UpdateTimerAsync(_cts.Token);
        return _cts.Token;
    }

    private void FinishRun()
    {
        _stopwatch?.Stop();
        IsRunning = false;
        IsPaused = false;
    }

    private async Task ProcessProxyAsync(
        ProxyCheckerService service,
        ProxyInfo proxy,
        CancellationToken ct)
    {
        try
        {
            await _pauseEvent.WaitAsync(ct).ConfigureAwait(false);

            var pending = await EnsurePendingResultAsync(proxy).ConfigureAwait(false);
            var result = await service.CheckProxyAsync(proxy, ct).ConfigureAwait(false);
            await UpdateUiWithResultAsync(pending, result).ConfigureAwait(false);

            if (AutoSave)
            {
                SaveSingleResult(result);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    private async Task UpdateUiWithResultAsync(CheckResult target, CheckResult result)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyResult(target, result);
            CompletedCount++;
            ProgressPercent = (double)CompletedCount / TotalCount * 100;

            if (result.Success)
            {
                SuccessCount++;
                if (!string.IsNullOrEmpty(result.RealIp) && _uniqueIps.Add(result.RealIp))
                {
                    UniqueIpCount++;
                }

                var group = IspIdentifier.GetIspGroup(result.Isp);
                switch (group)
                {
                    case "移动": MobileCount++; break;
                    case "电信": TelecomCount++; break;
                    case "联通": UnicomCount++; break;
                    default: OtherCount++; break;
                }
            }
            else
            {
                FailedCount++;
            }

            UpdateResultsSource();
        });
    }

    private async Task<CheckResult> EnsurePendingResultAsync(ProxyInfo proxy)
    {
        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existing = Results.FirstOrDefault(r => r.Proxy.Index == proxy.Index);
            if (existing != null)
            {
                return existing;
            }

            var pending = CheckResult.Pending(proxy);
            Results.Add(pending);
            UpdateResultsSource();
            return pending;
        });
    }

    private static void ApplyResult(CheckResult target, CheckResult source)
    {
        target.RealIp = source.RealIp;
        target.Isp = source.Isp;
        target.Location = source.Location;
        target.Success = source.Success;
        target.ResponseTimeMs = source.ResponseTimeMs;
        target.State = source.State;
        target.Error = source.Error;
    }

    [RelayCommand]
    private void Pause()
    {
        if (!IsRunning) return;

        if (IsPaused)
        {
            _pauseEvent.Set();
            IsPaused = false;
        }
        else
        {
            _pauseEvent.Reset();
            IsPaused = true;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
        _pauseEvent.Set();
    }

    [RelayCommand]
    private async Task ExportAllAsync()
    {
        if (_storageProvider == null || Results.Count == 0) return;

        // 显示导出选项对话框
        var optionsModel = new ExportOptionsModel();
        var dialogOptions = new OverlayDialogOptions
        {
            Buttons = DialogButton.None,
            CanLightDismiss = false,
            IsCloseButtonVisible = false,
        };

        var result = await OverlayDialog.ShowModal<ExportOptionsDialog, ExportOptionsModel>(
            optionsModel, options: dialogOptions);

        // 用户取消
        // 用户取消或点击外部关闭
        if (result != DialogResult.OK)
            return;

        // 过滤数据
        var query = optionsModel.OnlySuccess ? Results.Where(r => r.Success) : Results;

        // 生成导出内容
        var lines = query.Select(r =>
        {
            var parts = new List<string>();

            if (optionsModel.IncludeIp) parts.Add(r.Proxy.Ip);
            if (optionsModel.IncludePort) parts.Add(r.Proxy.Port.ToString());
            if (optionsModel.IncludeUsername) parts.Add(r.Proxy.Username ?? "");
            if (optionsModel.IncludePassword) parts.Add(r.Proxy.Password ?? "");
            if (optionsModel.IncludeRealIp) parts.Add(r.RealIp ?? "");
            if (optionsModel.IncludeLocation) parts.Add(r.Location ?? "");
            if (optionsModel.IncludeIsp) parts.Add(r.Isp ?? "");
            if (optionsModel.IncludeResponseTime) parts.Add(r.ResponseTimeDisplay);

            return string.Join(",", parts);
        }).ToList();

        if (lines.Count == 0) return;

        // 打开保存文件对话框
        var file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出结果",
            DefaultExtension = "txt",
            SuggestedFileName = "proxy_results.txt"
        });

        if (file == null) return;

        await File.WriteAllLinesAsync(file.Path.LocalPath, lines);
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        CompletedCount = 0;
        SuccessCount = 0;
        FailedCount = 0;
        UniqueIpCount = 0;
        MobileCount = 0;
        TelecomCount = 0;
        UnicomCount = 0;
        OtherCount = 0;
        ProgressPercent = 0;
        _uniqueIps.Clear();
        UpdateResultsSource();
    }

    [RelayCommand]
    private void ClearAll()
    {
        _cts?.Cancel();
        _pauseEvent.Set();

        _proxies.Clear();
        ClearResults();

        LoadedFileName = null;
        LoadedFilePath = null;
        TotalCount = 0;
        ElapsedTime = "00:00:00";
        Speed = "0个/秒";

        SearchText = string.Empty;
        FilterIsp = "全部";

        NotifyCanExecuteChanged();
    }

    private async Task UpdateTimerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && IsRunning)
        {
            await Task.Delay(1000, ct).ConfigureAwait(false);

            if (_stopwatch == null) continue;

            var elapsed = _stopwatch.Elapsed;
            var speedVal = elapsed.TotalSeconds > 0 ? CompletedCount / elapsed.TotalSeconds : 0;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ElapsedTime = elapsed.ToString(@"hh\:mm\:ss");
                Speed = $"{speedVal:F1}个/秒";
            });
        }
    }

    private void ClearOutputFiles()
    {
        var dir = LoadedFilePath ?? Environment.CurrentDirectory;
        string[] files = ["移动_proxies.txt", "电信_proxies.txt", "联通_proxies.txt", "其他_proxies.txt", "failed_proxies.txt"];

        foreach (var f in files)
        {
            var path = Path.Combine(dir, f);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> _tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public AsyncManualResetEvent(bool set)
        {
            if (set)
            {
                _tcs.TrySetResult(true);
            }
        }

        public Task WaitAsync(CancellationToken ct)
        {
            var task = _tcs.Task;
            return ct.CanBeCanceled ? task.WaitAsync(ct) : task;
        }

        public void Set() => _tcs.TrySetResult(true);

        public void Reset()
        {
            while (true)
            {
                var tcs = _tcs;
                if (!tcs.Task.IsCompleted)
                {
                    return;
                }

                var newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (Interlocked.CompareExchange(ref _tcs, newTcs, tcs) == tcs)
                {
                    return;
                }
            }
        }
    }

    private void SaveSingleResult(CheckResult result)
    {
        var dir = LoadedFilePath ?? Environment.CurrentDirectory;
        var p = result.Proxy;
        var line = $"{p.Ip},{p.Port},{p.Username},{p.Password}";

        lock (_fileLock)
        {
            if (result.Success)
            {
                var group = IspIdentifier.GetIspGroup(result.Isp);
                var filename = group switch
                {
                    "移动" => "移动_proxies.txt",
                    "电信" => "电信_proxies.txt",
                    "联通" => "联通_proxies.txt",
                    _ => "其他_proxies.txt"
                };
                File.AppendAllText(Path.Combine(dir, filename), line + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(
                    Path.Combine(dir, "failed_proxies.txt"),
                    $"{line} | {result.Error}{Environment.NewLine}");
            }
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (_clipboard == null || ResultsSource?.Selection == null) return;

        var selection = ResultsSource.Selection as ITreeDataGridRowSelectionModel<CheckResult>;
        if (selection == null || selection.SelectedItems.Count == 0) return;

        var text = string.Join(Environment.NewLine, selection.SelectedItems.Select(r =>
            string.IsNullOrEmpty(r.Proxy.Username)
                ? $"{r.Proxy.Ip}:{r.Proxy.Port}"
                : $"{r.Proxy.Ip}:{r.Proxy.Port}:{r.Proxy.Username}:{r.Proxy.Password}"));

        await _clipboard.SetTextAsync(text);
    }
}
