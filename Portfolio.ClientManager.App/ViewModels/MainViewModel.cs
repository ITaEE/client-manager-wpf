using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Portfolio.ClientManager.App.Commands;
using Portfolio.ClientManager.App.Services;
using Portfolio.ClientManager.Core.Csv;
using Portfolio.ClientManager.Core.Exceptions;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.App.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IClientService _clientService;
    private readonly IClientImportService _clientImportService;
    private readonly IClientCsvSerializer _csvSerializer;
    private readonly ICsvFileService _csvFileService;
    private readonly IUserDialogService _dialogService;
    private readonly AsyncRelayCommand _addCommand;
    private readonly AsyncRelayCommand _editCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private readonly AsyncRelayCommand _importCommand;
    private readonly AsyncRelayCommand _exportCommand;
    private readonly AsyncRelayCommand _clearFiltersCommand;
    private CancellationTokenSource? _searchCancellation;
    private Client? _selectedClient;
    private StatusFilterOption _selectedStatusFilter;
    private string _searchText = string.Empty;
    private string _statusText = "Ready";
    private bool _isBusy;
    private bool _suppressFilterRefresh;
    private int _displayedClientCount;
    private int _totalClientCount;

    public MainViewModel(
        IClientService clientService,
        IClientImportService clientImportService,
        IClientCsvSerializer csvSerializer,
        ICsvFileService csvFileService,
        IUserDialogService dialogService)
    {
        _clientService = clientService;
        _clientImportService = clientImportService;
        _csvSerializer = csvSerializer;
        _csvFileService = csvFileService;
        _dialogService = dialogService;

        StatusFilters =
        [
            new("All statuses", null),
            .. Enum.GetValues<ClientStatus>().Select(status => new StatusFilterOption(status.ToString(), status))
        ];
        _selectedStatusFilter = StatusFilters[0];

        _addCommand = new AsyncRelayCommand(AddClientAsync, () => !IsBusy);
        _editCommand = new AsyncRelayCommand(EditClientAsync, () => SelectedClient is not null && !IsBusy);
        _deleteCommand = new AsyncRelayCommand(DeleteClientAsync, () => SelectedClient is not null && !IsBusy);
        _importCommand = new AsyncRelayCommand(ImportClientsAsync, () => !IsBusy);
        _exportCommand = new AsyncRelayCommand(ExportClientsAsync, () => Clients.Count > 0 && !IsBusy);
        _clearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => HasActiveFilters && !IsBusy);
    }

    public ObservableCollection<Client> Clients { get; } = [];

    public IReadOnlyList<StatusFilterOption> StatusFilters { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? string.Empty))
            {
                OnFiltersChanged();
                if (!_suppressFilterRefresh)
                {
                    ScheduleSearch();
                }
            }
        }
    }

    public StatusFilterOption SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                OnFiltersChanged();
                if (!_suppressFilterRefresh)
                {
                    _ = LoadWithErrorHandlingAsync();
                }
            }
        }
    }

    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public int DisplayedClientCount
    {
        get => _displayedClientCount;
        private set
        {
            if (SetProperty(ref _displayedClientCount, value))
            {
                OnPropertyChanged(nameof(ClientCountText));
                OnPropertyChanged(nameof(IsListEmpty));
            }
        }
    }

    public int TotalClientCount
    {
        get => _totalClientCount;
        private set
        {
            if (SetProperty(ref _totalClientCount, value))
            {
                OnPropertyChanged(nameof(ClientCountText));
            }
        }
    }

    public string ClientCountText => $"Showing {DisplayedClientCount} of {TotalClientCount} client{(TotalClientCount == 1 ? string.Empty : "s")}";

    public bool IsListEmpty => DisplayedClientCount == 0;

    public string EmptyStateTitle => HasActiveFilters ? "No clients match the current filters" : "No clients yet";

    public string EmptyStateMessage => HasActiveFilters
        ? "Try changing the search text or clearing the status filter."
        : "Add your first client or import a CSV file.";

    public ICommand AddCommand => _addCommand;

    public ICommand EditCommand => _editCommand;

    public ICommand DeleteCommand => _deleteCommand;

    public ICommand ImportCommand => _importCommand;

    public ICommand ExportCommand => _exportCommand;

    public ICommand ClearFiltersCommand => _clearFiltersCommand;

    public Task InitializeAsync() => LoadClientsAsync();

    public void Dispose()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
    }

    private bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) || SelectedStatusFilter.Value is not null;

    private async Task AddClientAsync()
    {
        var input = _dialogService.ShowClientEditor(null);
        if (input is not null)
        {
            await RunOperationAsync(() => _clientService.CreateAsync(input), "Client added.");
        }
    }

    private async Task EditClientAsync()
    {
        var client = SelectedClient;
        if (client is null)
        {
            return;
        }

        var input = _dialogService.ShowClientEditor(client);
        if (input is not null)
        {
            await RunOperationAsync(() => _clientService.UpdateAsync(client.Id, input), "Changes saved.");
        }
    }

    private async Task DeleteClientAsync()
    {
        var client = SelectedClient;
        if (client is null || !_dialogService.ConfirmDelete(client))
        {
            return;
        }

        await RunOperationAsync(async () =>
        {
            await _clientService.DeleteAsync(client.Id);
            return client;
        }, "Client deleted.");
    }

    private async Task ExportClientsAsync()
    {
        var path = _dialogService.SelectCsvExportPath();
        if (path is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var clientsToExport = Clients.ToArray();
            var csv = await Task.Run(() => _csvSerializer.Serialize(clientsToExport));
            await _csvFileService.WriteAllTextAsync(path, csv);
            StatusText = $"Exported {clientsToExport.Length} client(s).";
            _dialogService.ShowInfo(
                $"Exported {clientsToExport.Length} client(s) from the current search and status filter.\n\n{path}");
        }
        catch (UnauthorizedAccessException)
        {
            ShowFileError("The CSV file could not be written because access was denied.");
        }
        catch (IOException)
        {
            ShowFileError("The CSV file could not be written. Check that the folder is available and the file is not in use.");
        }
        catch (Exception exception)
        {
            ShowOperationError(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ImportClientsAsync()
    {
        var path = _dialogService.SelectCsvImportPath();
        if (path is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var contents = await _csvFileService.ReadAllTextAsync(path);
            var readResult = await Task.Run(() => _csvSerializer.Parse(contents));
            if (readResult.Records.Count == 0)
            {
                _dialogService.ShowError(BuildNoRecordsMessage(readResult.Errors));
                StatusText = "Import cancelled: no valid CSV records.";
                return;
            }

            if (!_dialogService.ConfirmImport(readResult.Records.Count, readResult.Errors.Count))
            {
                StatusText = "Import cancelled.";
                return;
            }

            var importResult = await _clientImportService.ImportAsync(readResult.Records);
            await LoadClientsAsync();
            var message = BuildImportSummary(importResult, readResult.Errors, importResult.Errors);
            StatusText = $"Imported {importResult.ImportedCount} client(s).";
            _dialogService.ShowInfo(message);
        }
        catch (UnauthorizedAccessException)
        {
            ShowFileError("The CSV file could not be read because access was denied.");
        }
        catch (IOException)
        {
            ShowFileError("The CSV file could not be read. Check that it still exists and is not in use.");
        }
        catch (Exception exception)
        {
            ShowOperationError(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        _suppressFilterRefresh = true;
        SearchText = string.Empty;
        SelectedStatusFilter = StatusFilters[0];
        _suppressFilterRefresh = false;
        await LoadWithErrorHandlingAsync();
    }

    private async Task RunOperationAsync(Func<Task<Client>> operation, string successMessage)
    {
        try
        {
            IsBusy = true;
            await operation();
            await LoadClientsAsync();
            StatusText = successMessage;
        }
        catch (Exception exception)
        {
            ShowOperationError(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ScheduleSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        _ = SearchAfterDelayAsync(_searchCancellation.Token);
    }

    private async Task SearchAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(250, cancellationToken);
            await LoadClientsAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer search request replaced this one.
        }
        catch (Exception exception)
        {
            ShowOperationError(exception);
        }
    }

    private async Task LoadWithErrorHandlingAsync()
    {
        try
        {
            await LoadClientsAsync();
        }
        catch (Exception exception)
        {
            ShowOperationError(exception);
        }
    }

    private async Task LoadClientsAsync(CancellationToken cancellationToken = default)
    {
        var clientsTask = _clientService.SearchAsync(SearchText, SelectedStatusFilter.Value, cancellationToken);
        var totalCountTask = _clientService.GetTotalCountAsync(cancellationToken);
        await Task.WhenAll(clientsTask, totalCountTask);

        var clients = await clientsTask;
        Clients.Clear();
        foreach (var client in clients)
        {
            Clients.Add(client);
        }

        DisplayedClientCount = clients.Count;
        TotalClientCount = await totalCountTask;
        SelectedClient = null;
        _exportCommand.NotifyCanExecuteChanged();
    }

    private void OnFiltersChanged()
    {
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateMessage));
        _clearFiltersCommand.NotifyCanExecuteChanged();
    }

    private string BuildNoRecordsMessage(IReadOnlyList<CsvImportError> errors)
    {
        var detail = errors.Count == 0 ? string.Empty : $"\n\n{FormatErrors(errors)}";
        return $"No valid client records were found in the selected CSV file.{detail}";
    }

    private static string BuildImportSummary(
        CsvImportResult result,
        IReadOnlyList<CsvImportError> parseErrors,
        IReadOnlyList<CsvImportError> validationErrors)
    {
        var errors = parseErrors.Concat(validationErrors).ToArray();
        var skippedCount = result.DuplicateCount + errors.Length;
        var summary = $"Imported: {result.ImportedCount}\nSkipped: {skippedCount}\nDuplicates: {result.DuplicateCount}\nErrors: {errors.Length}";
        return errors.Length == 0 ? summary : $"{summary}\n\n{FormatErrors(errors)}";
    }

    private static string FormatErrors(IReadOnlyList<CsvImportError> errors)
    {
        var details = errors.Take(5).Select(error => $"Row {error.RowNumber}: {error.Message}");
        var remainder = errors.Count > 5 ? $"\n… and {errors.Count - 5} more error(s)." : string.Empty;
        return $"Details:\n{string.Join("\n", details)}{remainder}";
    }

    private void ShowFileError(string message)
    {
        _dialogService.ShowError(message);
        StatusText = "File operation failed.";
    }

    private void ShowOperationError(Exception exception)
    {
        var message = exception switch
        {
            ClientValidationException validationException => string.Join(Environment.NewLine, validationException.Errors.Values),
            KeyNotFoundException => exception.Message,
            _ => $"The operation could not be completed.\n\n{exception.Message}"
        };

        _dialogService.ShowError(message);
        StatusText = "Operation failed.";
    }

    private void NotifyCommandStates()
    {
        _addCommand.NotifyCanExecuteChanged();
        _editCommand.NotifyCanExecuteChanged();
        _deleteCommand.NotifyCanExecuteChanged();
        _importCommand.NotifyCanExecuteChanged();
        _exportCommand.NotifyCanExecuteChanged();
        _clearFiltersCommand.NotifyCanExecuteChanged();
    }
}
