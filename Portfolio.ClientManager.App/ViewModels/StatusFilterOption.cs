using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.App.ViewModels;

public sealed record StatusFilterOption(string Label, ClientStatus? Value);
