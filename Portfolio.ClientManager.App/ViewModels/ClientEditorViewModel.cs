using System.ComponentModel;
using System.Windows.Input;
using Portfolio.ClientManager.App.Commands;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Core.Validation;

namespace Portfolio.ClientManager.App.ViewModels;

public sealed class ClientEditorViewModel : ViewModelBase, IDataErrorInfo
{
    private readonly RelayCommand _saveCommand;
    private string _fullName;
    private string? _phone;
    private string? _email;
    private ClientStatus _status;
    private string? _notes;

    public ClientEditorViewModel(Client? client)
    {
        WindowTitle = client is null ? "Add client" : "Edit client";
        _fullName = client?.FullName ?? string.Empty;
        _phone = client?.Phone;
        _email = client?.Email;
        _status = client?.Status ?? ClientStatus.New;
        _notes = client?.Notes;

        _saveCommand = new RelayCommand(() => CloseRequested?.Invoke(true), () => IsValid);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(false));
    }

    public event Action<bool>? CloseRequested;

    public string WindowTitle { get; }

    public Array Statuses { get; } = Enum.GetValues<ClientStatus>();

    public string FullName
    {
        get => _fullName;
        set => SetValidatedProperty(ref _fullName, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetValidatedProperty(ref _phone, value);
    }

    public string? Email
    {
        get => _email;
        set => SetValidatedProperty(ref _email, value);
    }

    public ClientStatus Status
    {
        get => _status;
        set => SetValidatedProperty(ref _status, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetValidatedProperty(ref _notes, value);
    }

    public ICommand SaveCommand => _saveCommand;

    public ICommand CancelCommand { get; }

    public string Error => string.Join(Environment.NewLine, ClientValidator.Validate(ToInput()).Values);

    public string this[string columnName]
    {
        get
        {
            var errors = ClientValidator.Validate(ToInput());
            return errors.TryGetValue(columnName, out var error) ? error : string.Empty;
        }
    }

    public ClientInput ToInput() => new(FullName, Phone, Email, Status, Notes);

    private bool IsValid => ClientValidator.Validate(ToInput()).Count == 0;

    private void SetValidatedProperty<T>(
        ref T field,
        T value,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            _saveCommand.NotifyCanExecuteChanged();
        }
    }
}
