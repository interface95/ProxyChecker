using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Ursa.Controls;
using ProxyChecker.ViewModels;

namespace ProxyChecker.Views;

public partial class MainWindow : UrsaWindow
{
    private bool _isAnimating;

    public MainWindow()
    {
        InitializeComponent();
        if (DataContext is MainViewModel vm)
        {
            vm.SetStorageProvider(StorageProvider);
            if (Clipboard is not null) vm.SetClipboard(Clipboard);
        }

        // 订阅主题切换按钮点击事件
        if (TitleBarRightContent?.ThemeToggleButton is not null)
        {
            TitleBarRightContent.ThemeToggleButton.Tapped += OnThemeToggleClick;
        }
    }

    private async void OnThemeToggleClick(object? sender, TappedEventArgs e)
    {
        if (_isAnimating) return;
        e.Handled = true;

        var button = sender as Control;
        if (button == null) return;

        RenderTargetBitmap? bitmap = null;

        try
        {
            _isAnimating = true;

            // 1. 捕获当前视觉状态（旧主题）
            var pixelSize = new PixelSize((int)Bounds.Width, (int)Bounds.Height);
            bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
            bitmap.Render(this);

            ThemeOverlay.Source = bitmap;
            ThemeOverlay.Opacity = 1;
            ThemeOverlay.IsVisible = true;

            // 2. 设置径向渐变遮罩
            var centerPoint = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), this) ?? new Point(0, 0);

            var mask = new RadialGradientBrush
            {
                Center = new RelativePoint(centerPoint, RelativeUnit.Absolute),
                GradientOrigin = new RelativePoint(centerPoint, RelativeUnit.Absolute),
                RadiusX = new RelativeScalar(0, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(0, RelativeUnit.Relative),
                SpreadMethod = GradientSpreadMethod.Pad
            };

            mask.GradientStops = new GradientStops
            {
                new GradientStop(Colors.Transparent, 0),
                new GradientStop(Colors.Transparent, 0.99),
                new GradientStop(Colors.Black, 1)
            };

            ThemeOverlay.OpacityMask = mask;

            // 强制渲染延迟
            await Task.Delay(30);

            // 3. 切换主题（底层）
            var app = Application.Current;
            if (app is not null)
            {
                var isDark = app.ActualThemeVariant == ThemeVariant.Dark;
                app.RequestedThemeVariant = isDark ? ThemeVariant.Light : ThemeVariant.Dark;
            }

            // 等待主题应用
            await Task.Delay(50);

            // 4. 动画化遮罩扩散
            // 计算最大半径（像素）
            var corners = new[] { new Point(0, 0), new Point(Bounds.Width, 0), new Point(0, Bounds.Height), new Point(Bounds.Width, Bounds.Height) };
            double maxRadiusPx = 0;
            foreach (var p in corners)
            {
                var dist = Math.Sqrt(Math.Pow(p.X - centerPoint.X, 2) + Math.Pow(p.Y - centerPoint.Y, 2));
                if (dist > maxRadiusPx) maxRadiusPx = dist;
            }
            // 添加缓冲
            maxRadiusPx *= 1.2;

            var duration = TimeSpan.FromMilliseconds(250);
            var startTime = DateTime.Now;

            var w = Bounds.Width > 0 ? Bounds.Width : 1;
            var h = Bounds.Height > 0 ? Bounds.Height : 1;

            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (s, args) =>
            {
                var now = DateTime.Now;
                var elapsed = now - startTime;
                var t = Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
                var easedT = t * t; // 二次缓入

                var currentRadiusPx = easedT * maxRadiusPx;

                mask.RadiusX = new RelativeScalar(currentRadiusPx / w, RelativeUnit.Relative);
                mask.RadiusY = new RelativeScalar(currentRadiusPx / h, RelativeUnit.Relative);

                ThemeOverlay.InvalidateVisual();

                if (t >= 1)
                {
                    (s as DispatcherTimer)?.Stop();

                    // 清理
                    ThemeOverlay.Opacity = 0;
                    ThemeOverlay.IsVisible = false;
                    ThemeOverlay.Source = null;
                    ThemeOverlay.OpacityMask = null;

                    bitmap.Dispose();
                    _isAnimating = false;
                }
            });

            timer.Start();
        }
        catch (Exception)
        {
            ThemeOverlay.IsVisible = false;
            _isAnimating = false;
            bitmap?.Dispose();
        }
    }

    protected override async Task<bool> CanClose()
    {
        // 如果是更新后退出，跳过确认对话框
        if (App.SkipExitConfirmation)
        {
            return true;
        }

        var result = await MessageBox.ShowOverlayAsync("您确定要退出吗？", "退出提示", button: MessageBoxButton.YesNo, icon: MessageBoxIcon.Warning);
        return result == MessageBoxResult.Yes;
    }
}
