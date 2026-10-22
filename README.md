# ServiceHub IT

ServiceHub IT is an ASP.NET Core 8 MVC application for IT help desk, asset management, ticketing, and reporting.

## Overview

This application includes:
- Authentication and authorization with cookie-based login
- Ticket, asset, department, maintenance, notification, and knowledge base management
- Dashboard views and reporting features
- Supabase integration for backend data access
- PDF processing and document import support
- Machine learning ticket prediction model
- Responsive Bootstrap 5 UI and standard MVC layout


## Prerequisites

- .NET 8 SDK installed
- A Supabase project with REST API access
- Optional: Visual Studio 2022/2023, Visual Studio Code, or another C# editor

## Configuration

The application reads Supabase settings from configuration. Keep API keys out of source control; provide them with environment variables when running locally or through your deployment platform.

The required configuration section is:

```json
"Supabase": {
  "Url": "https://<your-supabase-project>.supabase.co",
  "ApiKey": "<your-supabase-api-key>",
  "RedirectUrl": "http://localhost:5080/Auth/Login"
}
```

Set the values in PowerShell before starting the app:

```powershell
$env:Supabase__Url = "https://<your-supabase-project>.supabase.co"
$env:Supabase__ApiKey = "<your-supabase-api-key>"
$env:Supabase__RedirectUrl = "http://localhost:5080/Auth/Login"
```

Use a Supabase anon/publishable key for client-facing application access; never use a service-role key in a client-facing app or commit secrets to source control.

## Run locally

1. Open a terminal in the folder containing `ServiceHub IT.csproj`:

```powershell
cd "path\to\ServiceHub-IT-main 1"
```

2. Restore packages and run the app on the documented local URL:

```powershell
dotnet restore
dotnet run --urls http://localhost:5080
```

3. Open the browser at:

```text
http://localhost:5080
```

The default application route opens the login page.

## Project structure

- `Controllers/` - MVC controllers for pages and API actions
- `Models/` - domain models, view models, and configuration classes
- `Views/` - Razor views and shared layouts
- `Services/` - application services, Supabase integration, PDF processing, reporting, and ML
- `Repositories/` - data access and repository base classes
- `Interfaces/` - service and repository interfaces
- `DTOs/` - data transfer objects
- `Utilities/` - shared constants and helper code
- `Middleware/` - request logging and pipeline middleware
- `wwwroot/` - static assets, CSS, JS, and images

## Key packages

- `Supabase` for Supabase REST API integration
- `Microsoft.ML` for machine learning ticket prediction
- `DocumentFormat.OpenXml` for working with Office documents
- `UglyToad.PdfPig` for PDF parsing and processing

## Notes

- The app uses cookie authentication; HTTPS is recommended for deployed environments.
- Authorization is enabled globally, so users must sign in to access protected routes.
- If you change the local port or hostname, update `Supabase:RedirectUrl` accordingly.

## Troubleshooting

- If the app does not start, verify your .NET SDK installation with:

```powershell
dotnet --info
```

- If login or Supabase fails, confirm the `Supabase` section in `appsettings.json` is correct.
- If you use a different development port, update the redirect URL in configuration.
