using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Portfolio.ClientManager.App.ViewModels;

namespace Portfolio.ClientManager.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && e.OriginalSource is not TextBox && _viewModel.DeleteCommand.CanExecute(null))
        {
            _viewModel.DeleteCommand.Execute(null);
            e.Handled = true;
        }
    }
}
