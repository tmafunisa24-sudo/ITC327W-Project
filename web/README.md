# ServiceHub IT

ServiceHub IT is an ASP.NET Core 8 MVC application for IT help-desk requests, ticketing, asset and department management, maintenance, notifications, knowledge-base content, dashboards, and reporting.

## Prerequisites

- .NET 8 SDK
- Access to a configured Supabase project for backend features
- PowerShell for the local configuration examples below

Verify the SDK installation:

```powershell
dotnet --version
```

## Local configuration

The application binds the `Supabase` configuration section. Supply values locally with environment variables or .NET User Secrets; do not put credentials in `appsettings*.json`, source control, screenshots, or pull requests.

Required settings:

- `Supabase__Url` - Supabase project URL.
- `Supabase__ApiKey` - anon/publishable key for the project.
- `Supabase__RedirectUrl` - local auth redirect URL; keep it consistent with the redirect URLs configured in Supabase.

`Supabase__ServiceRoleKey` is an optional, highly privileged server-side setting. Do not set it unless a reviewed server-side operation specifically requires it. Never expose it to browser code or commit it; service-role access can bypass row-level security.

Optional storage bucket overrides are `Supabase__TicketScreenshotBucketName`, `Supabase__AssetDocumentBucketName`, `Supabase__AssetImageBucketName`, and `Supabase__AssetWarrantyBucketName`. The application provides defaults for these names.

For a temporary PowerShell session, enter values at prompts so they are not written as literals in the command history:

```powershell
$env:Supabase__Url = Read-Host "Supabase project URL"
$secureApiKey = Read-Host -AsSecureString "Supabase anon/publishable API key"
$env:Supabase__ApiKey = [System.Net.NetworkCredential]::new("", $secureApiKey).Password
$env:Supabase__RedirectUrl = "http://localhost:5080/Auth/Login"
```

Environment variables set this way apply only to the current PowerShell process and child processes. Close the terminal when finished. For persistent local development, .NET User Secrets can be used from the project directory:

```powershell
dotnet user-secrets init --project ".\ServiceHub IT.csproj"
dotnet user-secrets set "Supabase:Url" "<project-url>" --project ".\ServiceHub IT.csproj"
dotnet user-secrets set "Supabase:ApiKey" "<anon-or-publishable-key>" --project ".\ServiceHub IT.csproj"
dotnet user-secrets set "Supabase:RedirectUrl" "http://localhost:5080/Auth/Login" --project ".\ServiceHub IT.csproj"
```

Replace placeholders locally; never commit actual values. Do not add a service-role key unless its use has been reviewed and it can remain strictly server-side.

## Supabase setup

1. Use a development Supabase project and configure its URL and anon/publishable key locally.
2. Configure the local auth redirect URL in the Supabase project's authentication settings.
3. Ensure the database schema, access policies, and storage buckets required by the application are configured using the group's reviewed database documentation.
4. Keep row-level security enabled and review policies before use. Do not connect these instructions to production data or disable database security to work around configuration errors.

## Restore, build, test, and run

Run commands from this directory, next to `ServiceHub IT.csproj`:

```powershell
dotnet restore ".\ServiceHub IT.csproj"
dotnet build ".\ServiceHub IT.csproj"
dotnet test
dotnet run --project ".\ServiceHub IT.csproj" --urls "http://localhost:5080"
```

No automated test project was present when this integration was prepared. If a test project is added, run it explicitly with `dotnet test <path-to-test-project.csproj>`. Do not report unexecuted tests as passing.

Open `http://localhost:5080`; the default route is the login page. The environment variables configured in the same PowerShell session will be available to the application.

## Project structure

- `Controllers/` - MVC controllers and application endpoints
- `DTOs/`, `Models/` - transfer, domain, and view models
- `Filters/`, `Middleware/` - request authorization and pipeline behavior
- `Interfaces/`, `Repositories/`, `Services/` - application contracts and backend logic
- `Utilities/` - shared helpers
- `Views/` - Razor pages and shared layouts
- `wwwroot/` - static CSS, JavaScript, and image assets

## Troubleshooting

- If `dotnet --version` reports that no SDK is installed, install the .NET 8 SDK and open a new terminal before restoring or building.
- If restore fails, check NuGet connectivity and rerun `dotnet restore`.
- If Supabase requests fail, verify the local URL and anon/publishable key, auth redirect configuration, schema, policies, and bucket names. Never paste keys into logs or issue reports.
- If a request is unauthorized, sign in and check the relevant Supabase policy rather than disabling authentication or row-level security.
- If the local port changes, update `Supabase__RedirectUrl` and the corresponding Supabase auth redirect configuration.
