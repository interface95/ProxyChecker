using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ProxyChecker.Dialogs.Views;

public partial class UpdateDialog : UserControl
{
    public UpdateDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
