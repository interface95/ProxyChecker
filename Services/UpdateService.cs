using System;
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
        // Explicitly use GithubSource
        var source = new GithubSource(RepoUrl, null, false);
        _updateManager = new UpdateManager(source);
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (_updateManager == null) await InitializeAsync();

        try
        {
            _updateInfo = await _updateManager!.CheckForUpdatesAsync();
            return _updateInfo;
        }
        catch
        {
            // Log error or ignore
            return null;
        }
    }

    public async Task DownloadUpdatesAsync(Action<int> progress)
    {
        if (_updateManager == null || _updateInfo == null) return;

        await _updateManager.DownloadUpdatesAsync(_updateInfo, progress);
    }

    public void ApplyUpdatesAndRestart()
    {
        if (_updateManager == null || _updateInfo == null) return;

        _updateManager.WaitExitThenApplyUpdates(_updateInfo);
    }
}
