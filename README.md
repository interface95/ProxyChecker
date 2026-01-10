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

- **Windows**: 下载 `ProxyChecker-win-x64.zip`
- **macOS**: 下载 `ProxyChecker-osx-x64.tar.gz` (Intel) 或 `ProxyChecker-osx-arm64.tar.gz` (Apple Silicon)
- **Linux**: 下载 `ProxyChecker-linux-x64.tar.gz`

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

## 📝 许可证

本项目采用 [MIT 许可证](LICENSE)。
