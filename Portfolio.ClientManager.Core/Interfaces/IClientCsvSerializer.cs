using Portfolio.ClientManager.Core.Csv;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Interfaces;

public interface IClientCsvSerializer
{
    string Serialize(IEnumerable<Client> clients);

    CsvReadResult Parse(string csv);
}
