using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ProxyChecker.Services;

public class UpdateService
{
    private const string RepoUrl = "https://github.com/interface95/ProxyChecker";
    private UpdateManager? _updateManager;
    private UpdateInfo? _updateInfo;

    public bool IsSupported => true;

    public async Task InitializeAsync()
    {
        // Explicitly use GithubSource, allow prerelease versions
        var source = new GithubSource(RepoUrl, null, true);
        _updateManager = new UpdateManager(source);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (_updateManager == null) await InitializeAsync();

        return await _updateManager!.CheckForUpdatesAsync();
    }

    public async Task DownloadUpdatesAsync(Action<int> progress, CancellationToken cancellationToken = default)
    {
        if (_updateManager == null || _updateInfo == null)
            throw new InvalidOperationException("UpdateManager or UpdateInfo is not initialized. Please check for updates first.");

        try
        {
            await _updateManager.DownloadUpdatesAsync(_updateInfo, progress, cancelToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"下载更新失败: {ex.Message}", ex);
        }
    }

    public void ApplyUpdatesAndRestart()
    {
        if (_updateManager == null || _updateInfo == null) return;

        // 应用更新并重启应用
        _updateManager.ApplyUpdatesAndRestart(_updateInfo);
    }
}
