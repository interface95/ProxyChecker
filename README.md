# ProxyChecker

一个基于 Avalonia UI 和 .NET 10 的高性能现代化代理检测工具 (SOCKS5/HTTP)。

![License](https://img.shields.io/github/license/interface95/ProxyChecker)
![Build Status](https://img.shields.io/github/actions/workflow/status/interface95/ProxyChecker/release.yml)

## ✨ 特性

- **高性能检测**: 使用 `System.IO.Pipelines` 和异步并发进行极速代理验证。
- **现代化 UI**: 采用 Semi Design 风格，支持深色模式，界面美观流畅。
- **多平台支持**: 完美支持 Windows, macOS, Linux (x64/ARM64)。
- **AOT 编译**: 提供 Native AOT 版本，无需安装 .NET 运行时，启动速度极快。
- **功能丰富**:
    - 支持 SOCKS5 和 HTTP 代理检测。
    - 智能识别 ISP 和地理位置。
    - 支持拖拽导入文件。
    - 结果列表自动滚动。
    - 底部状态栏显示实时进度。
    - 右键菜单支持复制、导出、清空等操作。

## 🚀 快速开始

### 下载

请前往 [Releases](https://github.com/interface95/ProxyChecker/releases) 页面下载对应系统的最新版本。

- **Windows**: 下载 `ProxyChecker-windows-x64-installer.exe` (x64) 或 `ProxyChecker-windows-arm64-installer.exe` (ARM64)
- **macOS**: 下载 `ProxyChecker-macos-x64-installer.pkg` (Intel) 或 `ProxyChecker-macos-arm64-installer.pkg` (Apple Silicon)
- **Linux**: 下载 `ProxyChecker-linux-x64.tar.gz` 或 `ProxyChecker-linux-arm64.tar.gz`

### 运行

解压后直接运行可执行文件 `ProxyChecker` 即可。

## 🛠️ 构建

如果你想从源码构建：

1.  **环境要求**:
    - .NET 10.0 SDK
    - (可选) Native AOT 编译环境 (C++ 构建工具)

2.  **克隆项目**:
    ```bash
    git clone https://github.com/interface95/ProxyChecker.git
    cd ProxyChecker
    ```

3.  **运行**:
    ```bash
    dotnet run --project ProxyChecker.csproj
    ```

4.  **发布 (AOT)**:
    ```bash
    dotnet publish -r win-x64 -c Release -p:PublishAot=true
    ```

## 📦 版本管理

本项目使用 [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) 进行版本管理。

### 版本号规则

| Git 状态 | 版本格式 | 说明 |
|----------|----------|------|
| 无 tag（开发中） | `v1.0.0-preview.20` | 预览版本，包含 commit 计数 |
| 有 tag `v1.0.0-preview` | `v1.0.0-preview` | 预览发布版本 |
| 有 tag `v1.0.0` | `v1.0.0` | 正式发布版本 |

### 发布新版本

发布新版本需要创建并推送 Git tag：

```bash
# 1. 更新代码并提交
git add .
git commit -m "Release v1.0.1"

# 2. 创建 tag (预览版加上 -preview 后缀)
git tag v1.0.1-preview

# 3. 推送 tag（触发 GitHub Actions 自动构建）
git push origin main
git push origin v1.0.1-preview
```

推送 tag 后，GitHub Actions 会自动：
- 构建 Windows / macOS / Linux (x64/ARM64) 平台
- 进行 Native AOT 编译
- 打包 Velopack 安装包
- 创建 GitHub Release

## 📝 许可证

本项目采用 [MIT 许可证](LICENSE)。
