using System.Threading.Tasks;
using Ursa.Controls;
using ProxyChecker.ViewModels;

namespace ProxyChecker.Views;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();
        if (DataContext is MainViewModel vm)
        {
            vm.SetStorageProvider(StorageProvider);
            if (Clipboard is not null) vm.SetClipboard(Clipboard);
        }
    }

    protected override async Task<bool> CanClose()
    {
        // 如果是更新后退出，跳过确认对话框
        if (App.SkipExitConfirmation)
        {
            return true;
        }

        var result = await MessageBox.ShowOverlayAsync("您确定要退出吗？", "退出提示", button: MessageBoxButton.YesNo);
        return result == MessageBoxResult.Yes;
    }
}
