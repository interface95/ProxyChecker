using Avalonia.Controls;
using Ursa.Controls;

namespace ProxyChecker.Views;

public partial class TitleBarRightContent : UserControl
{
    public TitleBarRightContent()
    {
        InitializeComponent();
    }

    // 暴露给 MainWindow 访问，Avalonia 源生成器会自动生成 ThemeToggleButton 属性
}
