using System.Windows;
using Portfolio.ClientManager.App.ViewModels;

namespace Portfolio.ClientManager.App.Views;

public partial class ClientEditorWindow : Window
{
    private readonly ClientEditorViewModel _viewModel;

    public ClientEditorWindow(ClientEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;
        Closed += OnClosed;
    }

    private void OnCloseRequested(bool result) => DialogResult = result;

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        Closed -= OnClosed;
    }
}
