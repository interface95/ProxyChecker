namespace ProxyChecker.Services;

/// <summary>
/// 版本信息类，在 CI 构建时通过 GitVersionInfo.g.cs 更新
/// </summary>
public static partial class VersionInfo
{
    /// <summary>
    /// 完整的 Git 标签版本 (如 v1.0.15-preview)
    /// 构建时通过 scripts/set-version.ps1 更新
    /// </summary>
    public const string GitTag = "v1.0.15-preview";

    /// <summary>
    /// 获取显示版本号
    /// </summary>
    public static string GetDisplayVersion()
    {
        return !string.IsNullOrEmpty(GitTag) ? GitTag : "v1.0.0-dev";
    }
}
