using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.App.Services;

public interface IUserDialogService
{
    ClientInput? ShowClientEditor(Client? client);

    bool ConfirmDelete(Client client);

    bool ConfirmPotentialDuplicate(IReadOnlyList<Client> potentialDuplicates);

    string? SelectCsvImportPath();

    string? SelectCsvExportPath();

    bool ConfirmImport(int recordCount, int parseErrorCount);

    void ShowInfo(string message);

    void ShowError(string message);
}
