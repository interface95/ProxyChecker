using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ProxyChecker.Dialogs.ViewModels;

namespace ProxyChecker.Dialogs.Views;

public partial class UpdateDialog : UserControl
{
    public UpdateDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateViewModel.IsUpdateDialogOpen = true;
        Unloaded += (s, e) => UpdateViewModel.IsUpdateDialogOpen = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
