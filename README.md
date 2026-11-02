# Click Computer and Accessories - Client Support Platform

This is our Group T project for ITC327W. It has the work for all three phases: planning and requirements (Phase 1), design (Phase 2) and the actual system we built and demonstrated (Phase 3).

The app used to be called ServiceHub IT, so you might still see that name in some files and in the web project name.

## What the project is about

Click Computer and Accessories wanted an easier way to handle support requests. The platform lets clients send in a request for a computer or electronic device, and lets support staff, technicians and admins deal with it. We wanted it to be easier to:

- submit a request for a device
- follow the progress of a request and talk to support staff
- give requests to technicians and keep track of their workload
- let admins see all users, requests and assignments in one place

## The three phases

**Phase 1 (24 Aug to 8 Sep 2026)**
We spoke to the stakeholder and worked out the problem. We wrote the SRS (final version of Phase 1 was version 2.0), did the feasibility study and the risk register, made the Microsoft Project plan and set up this GitHub repository. Phase 1 was submitted and presented.

**Phase 2 (9 to 23 Sep 2026)**
We designed the system before building anything. This covered the system architecture, UML diagrams, the database design (ERD and Supabase), the access and security design and a prototype of the interface. We got feedback from the stakeholder and changed the design where needed. We also did the requirements traceability and updated the SRS and project plan.

**Phase 3 (10 Oct to 3 Nov 2026)**
We built the system and joined the pieces together. The tasks in the project plan were:

- 3.1 Set up the Supabase database (tables, fields, keys, relationships)
- 3.2 Set up authentication and access in Supabase
- 3.3 Build the Flutter screens and navigation
- 3.4 Connect Flutter to Supabase
- 3.5 Build the ASP.NET web pages and navigation
- 3.6 Connect ASP.NET to Supabase
- 3.7 Shared Process 1: submitting and retrieving a request
- 3.8 Shared Process 2: updating and showing the status
- 3.14 Update the Microsoft Project plan
- 3.15 Update the SRS with what was done and what is still missing
- 3.16 Organise the code, README and documents on GitHub
- 3.17 Final demonstration (3 Nov 2026)

Task 3.9 (making the data fields and status values the same in both apps) was also done.

## What was built

The system has three parts that all use the same Supabase project:

- Supabase for the database and sign-in
- a Flutter mobile app in `mobile/` for clients
- an ASP.NET Core 8 MVC web app in `web/` for staff, technicians and admins (cookie sign-in)

The main workflow is: client signs in, picks the device and the problem, submits the request, and then follows the status. On the web side a technician or admin can see the request, assign it and change the status. Notifications, device details and file attachments were extras that we added where we had time.

The two shared processes we showed in the demo:

1. A request made in one app shows up in the other app.
2. A status change or assignment made on the web shows up on the mobile app.

## Technology

- ASP.NET Core 8 MVC and C#
- Flutter (Dart)
- Supabase
- Bootstrap for the web pages
- Microsoft ML, Open XML and PDF packages in the web project
- Git/GitHub and Microsoft Project

## Running the web app

You need:

- .NET 8 SDK
- a development Supabase project set up for the app
- PowerShell (the examples below use it)

Check the SDK is installed:

```powershell
dotnet --version
```

### Supabase settings

The web app reads these settings from the Supabase section of the configuration:

| Setting | What it is for |
|---|---|
| Supabase__Url | Supabase project URL |
| Supabase__ApiKey | Supabase anon/publishable key |
| Supabase__RedirectUrl | local sign-in redirect URL |

For one PowerShell session you can type them in like this:

```powershell
$env:Supabase__Url = Read-Host "Supabase project URL"
$secureApiKey = Read-Host -AsSecureString "Supabase anon/publishable API key"
$env:Supabase__ApiKey = [System.Net.NetworkCredential]::new("", $secureApiKey).Password
$env:Supabase__RedirectUrl = "http://localhost:5080/Auth/Login"
```

The app only sees these if you start it from the same PowerShell window. If you want them to stay, use .NET User Secrets instead. `web/README.md` has the full setup, storage settings and troubleshooting.

Use a development project and the anon/publishable key. Never commit keys or passwords, and never put the Supabase service-role key in code that runs in the browser.

### Build and run

From the root of the repository:

```powershell
dotnet restore ".\web\ServiceHub IT.csproj"
dotnet build ".\web\ServiceHub IT.csproj"
dotnet run --project ".\web\ServiceHub IT.csproj" --urls "http://localhost:5080"
```

Then open http://localhost:5080, which goes to the sign-in page. The same redirect URL has to be added in the authentication settings of your Supabase project.

## Running the mobile app

The Flutter app is in `mobile/` and uses the same Supabase project. You need the Flutter SDK and an emulator or a phone.

```powershell
cd mobile
flutter pub get
flutter run
```

The Supabase details are set up locally, see `mobile/README.md`. If Flutter complains about the version, tell the project manager and do not change `pubspec.yaml` to hide the error.

## Testing

We do not have an automated test project. We tested the functions by hand with dummy data. If a test project gets added later it can be run with `dotnet test <path-to-test-project.csproj>`.

## Folders

- `docs/` - requirements, stakeholder and feasibility documents, risks, architecture, meeting records and evidence
- `web/` - the ASP.NET Core MVC app
- `mobile/` - the Flutter app
- `database/` - database files
- `testing/` - testing material
- `project_management/` - project plans and progress trackers

## Documents

- Documentation index
- System Requirements Specification (Phases 1 to 3)
- Phase 1: feasibility study and risk register
- Phase 2: system architecture, UML, ERD and interface prototype
- Phase 3: Microsoft Project plan and the Phase 3 progress tracker

## Group T

| Name | Main job |
|---|---|
| Lesako M | Project manager, GitHub |
| Tladi K | Final demonstration |
| Nosenga B | Flutter mobile app and Supabase connection |
| Mafunisa T | ASP.NET web pages and navigation |
| Hlalele D | SRS |
| Ndlovu N | Shared Process 1 |
| Taaibosch K | Shared Process 2 |
| Molefe sb | Supabase database |
| Lesenyeho lj | Microsoft Project plan |
| Winkel k | Connecting Supabase to the web app |

Stakeholder: Click Computer and Accessories. Mentor: Mpho Mbele.

## Known problems

- There are no automated tests.

