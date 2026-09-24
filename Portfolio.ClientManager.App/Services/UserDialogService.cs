using System.Windows;
using Microsoft.Win32;
using Portfolio.ClientManager.App.ViewModels;
using Portfolio.ClientManager.App.Views;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.App.Services;

public sealed class UserDialogService : IUserDialogService
{
    public ClientInput? ShowClientEditor(Client? client)
    {
        var viewModel = new ClientEditorViewModel(client);
        var window = new ClientEditorWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true ? viewModel.ToInput() : null;
    }

    public bool ConfirmDelete(Client client)
    {
        return MessageBox.Show(
            Application.Current.MainWindow,
            $"Delete {client.FullName}? This action cannot be undone.",
            "Delete client",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public bool ConfirmPotentialDuplicate(IReadOnlyList<Client> potentialDuplicates)
    {
        var names = potentialDuplicates
            .Take(3)
            .Select(client => $"• {client.FullName}");
        var remainder = potentialDuplicates.Count > 3
            ? $"\n• and {potentialDuplicates.Count - 3} more"
            : string.Empty;

        return MessageBox.Show(
            Application.Current.MainWindow,
            $"A client with the same email address or phone number may already exist:\n\n{string.Join("\n", names)}{remainder}\n\nSave this client anyway?",
            "Potential duplicate",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public string? SelectCsvImportPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import clients from CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }

    public string? SelectCsvExportPath()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export clients to CSV",
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = ".csv",
            AddExtension = true,
            FileName = $"clients-{DateTime.Now:yyyy-MM-dd}.csv",
            OverwritePrompt = true
        };

        return dialog.ShowDialog(Application.Current.MainWindow) == true ? dialog.FileName : null;
    }

    public bool ConfirmImport(int recordCount, int parseErrorCount)
    {
        var parseNote = parseErrorCount == 0
            ? string.Empty
            : $"\n\n{parseErrorCount} malformed row(s) will be skipped.";
        return MessageBox.Show(
            Application.Current.MainWindow,
            $"Import {recordCount} valid CSV record(s)?{parseNote}",
            "Import clients",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.Yes) == MessageBoxResult.Yes;
    }

    public void ShowInfo(string message)
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            message,
            "Client Manager",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowError(string message)
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            message,
            "Client Manager",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
