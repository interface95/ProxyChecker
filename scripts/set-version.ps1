# 版本信息更新脚本 - CI 构建时调用
# 从 Git 标签更新 VersionInfo.cs

$ErrorActionPreference = "Stop"

# 获取当前 Git 标签
$tag = git describe --tags --abbrev=0 2>$null

if ([string]::IsNullOrEmpty($tag)) {
    Write-Host "No git tag found, using default version"
    $tag = "v1.0.0-dev"
}

Write-Host "Current tag: $tag"

# 更新 VersionInfo.cs 文件
$versionFile = "Services/VersionInfo.cs"
$content = Get-Content $versionFile -Raw

# 替换 GitTag 常量的值
$newContent = $content -replace 'public const string GitTag = "[^"]*";', "public const string GitTag = `"$tag`";"

Set-Content $versionFile -Value $newContent -NoNewline

Write-Host "✓ Updated VersionInfo.GitTag to $tag"
