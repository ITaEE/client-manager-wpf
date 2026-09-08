# Client Manager

A lightweight local desktop application for managing customer records, built with WPF and .NET 8. Client Manager keeps contact details, relationship status, and notes in a local SQLite database—no account or cloud service required.

## Screenshots

![Main client management interface](docs/screenshots/main-window.png)

_Main client management interface_

![Client editing dialog](docs/screenshots/edit-client.png)

_Client editing dialog_

## Features

- Create, edit, and delete clients
- Search by name, phone, and email
- Filter by client status
- Sort client records
- CSV import and export
- Input validation
- Local SQLite persistence
- Asynchronous data access
- Empty states and user-friendly error handling

## Technology Stack

- C# / .NET 8
- WPF
- MVVM
- Entity Framework Core
- SQLite
- Microsoft.Extensions.DependencyInjection
- xUnit

## Architecture

The solution separates presentation, domain logic, persistence, and tests:

- `Portfolio.ClientManager.App` — WPF views, view models, commands, dialogs, and startup configuration
- `Portfolio.ClientManager.Core` — domain models, validation, service contracts, and CSV processing
- `Portfolio.ClientManager.Infrastructure` — EF Core, SQLite, and repository implementations
- `Portfolio.ClientManager.Tests` — unit and SQLite integration tests

## Getting Started

### Requirements

- Windows
- .NET 8 SDK

```powershell
dotnet restore
dotnet build
dotnet run --project Portfolio.ClientManager.App
```

## Tests

Run the automated test suite with:

```powershell
dotnet test
```

The suite contains 16 tests covering client operations, validation, search and filtering, CSV processing, duplicate handling, and SQLite persistence.

## Data Storage

Client data is stored locally in SQLite at:

```text
%LOCALAPPDATA%\Portfolio.ClientManager\client-manager.db
```

The application creates the directory and database on first launch.

## CSV Import / Export

The current search and status-filtered list can be exported as UTF-8 CSV. The importer supports the same format, validates each row, and continues after malformed or invalid entries.

Duplicate rows are skipped when normalized full name, phone, email, status, and notes match an existing client or an earlier row in the same import. Matching is case-insensitive and ignores leading and trailing whitespace.

## Portfolio Highlights

- WPF desktop development
- MVVM architecture
- Dependency Injection
- EF Core and SQLite
- Asynchronous programming
- Validation and file processing
- Automated testing
- Layered application design

## Project Status

Portfolio project — feature complete for the current release.

## Roadmap

Possible future improvements:

- Dark theme
- Advanced filtering
- Client history
- MSIX installer

## License

Distributed under the [MIT License](LICENSE).
