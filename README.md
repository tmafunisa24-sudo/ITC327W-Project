# Click Computer and Accessories - Client Support Platform

This repository contains the Phase 3 development work for the Click Computer and Accessories client-support platform. The project builds on the requirements and architecture documented in earlier phases to provide a central place for clients to submit support requests and for support staff, technicians, and administrators to manage them.

The application was previously called **ServiceHub IT**.

## Project goals

The platform aims to improve the current support process by making it easier to:

- Submit requests for computer and electronic devices.
- Track request progress and communicate with support staff.
- Assign requests to technicians and manage their workload.
- Give administrators a central view of users, requests, and assignments.

## Scope and current implementation

The approved core workflow covers client sign-in, device and problem selection, request submission, status tracking, support communication, technician assignment, and administrator controls. Notifications, device or asset details, and file attachments are additional requirements where included in the final implementation.

The current working application is an **ASP.NET Core 8 MVC web app** in [`web/`](web/). It uses cookie-based sign-in and is configured to access Supabase services. The architecture documents also describe a Flutter mobile client and an ASP.NET API; those are part of the planned integrated solution, but the `mobile/`, `database/`, and `testing/` folders in this repository currently contain placeholders rather than implementation or test projects.

## Technology

- ASP.NET Core 8 MVC and C#
- Supabase for backend data services
- Bootstrap-based web interface
- Microsoft ML, Open XML, and PDF processing packages in the web project

## Run the web application

### Prerequisites

- .NET 8 SDK
- A development Supabase project configured for the application
- PowerShell (for the examples below)

Check that the SDK is available:

```powershell
dotnet --version
```

### Configure Supabase

The web app reads the following settings from the `Supabase` configuration section:

| Setting | Purpose |
| --- | --- |
| `Supabase__Url` | Supabase project URL |
| `Supabase__ApiKey` | Supabase anon/publishable key |
| `Supabase__RedirectUrl` | Local authentication redirect URL |

For a temporary PowerShell session, enter the project URL and key locally:

```powershell
$env:Supabase__Url = Read-Host "Supabase project URL"
$secureApiKey = Read-Host -AsSecureString "Supabase anon/publishable API key"
$env:Supabase__ApiKey = [System.Net.NetworkCredential]::new("", $secureApiKey).Password
$env:Supabase__RedirectUrl = "http://localhost:5080/Auth/Login"
```

These environment variables are available to the app started from the same PowerShell session. For persistent local development, use .NET User Secrets. See [`web/README.md`](web/README.md) for the full configuration, Supabase setup, storage settings, and troubleshooting instructions.

Use a development project and an anon/publishable key. Never commit credentials or expose a Supabase service-role key to browser code.

### Restore, build, and run

Run these commands from the repository root:

```powershell
dotnet restore ".\web\ServiceHub IT.csproj"
dotnet build ".\web\ServiceHub IT.csproj"
dotnet run --project ".\web\ServiceHub IT.csproj" --urls "http://localhost:5080"
```

Open [http://localhost:5080](http://localhost:5080). The default route opens the sign-in page. Configure the same redirect URL in the Supabase project's authentication settings.

There is no automated test project in the repository yet. When one is added, run it with `dotnet test <path-to-test-project.csproj>`.

## Repository layout

- [`docs/`](docs/) - requirements, stakeholder and feasibility documents, risks, architecture, meeting records, and project evidence
- [`web/`](web/) - ASP.NET Core MVC application
- [`mobile/`](mobile/) - reserved for the mobile client
- [`database/`](database/) - reserved for database-related project files
- [`testing/`](testing/) - reserved for test projects and testing materials
- [`project_management/`](project_management/) - project plans and progress trackers

## Project documents

- [Documentation index](docs/README.md)
- [System Requirements Specification](<docs/stakeholder/Click Computer and accessories SRS (Phase 1&2).pdf>)
- [Phase 2 system architecture](<docs/architecture/Phase 2 -System Architecture.pdf>)
- [Architecture overview](docs/architecture/architecture_overview.md)
- [Risk register](docs/risks/risk_register.md)
