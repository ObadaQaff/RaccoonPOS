# AGENTS.md

## Scope
These instructions apply to the repository rooted at `C:\Users\obadaqafisheh\source\repos\ROCCOPOS`.

## Workflow Rules
- Before making any change, show a preview first.
- List all files that will be created, edited, moved, or deleted.
- List the commands that will be run.
- Wait for my approval before making changes.

## Command Conventions
- If the user message starts with `/p`, enter planning behavior.
- In `/p` mode, do not edit files yet.
- In `/p` mode, provide a technical plan, assumptions, risks, and recommended approach.
- In `/p` mode, answer technical questions in depth when needed.
- Wait for user approval before making code or file changes.

## Safety Rules
- Do not use destructive commands unless I explicitly approve them.
- Do not modify unrelated files.

## Response Style
- Keep responses concise.
- Explain what changed after the work is done.

## Repository Shape
- Main solution: `RaccoonWarehouse-master/RaccoonWarehouse.sln`
- Main desktop app: `RaccoonWarehouse-master/RaccoonWarehouse.csproj`
- Supporting projects:
  - `RaccoonWarehouse.Application`
  - `RaccoonWarehouse.Core`
  - `RaccoonWarehouse.Data`
  - `RaccoonWarehouse.Domain`

## Architecture
- This is a .NET 8 WPF desktop application.
- `RaccoonWarehouse-master` contains WPF windows, XAML, startup wiring, navigation, reports, and feature folders such as `Invoices`, `Products`, `Stocks`, `Reports`, `Auth`, and `FinancialTransactions`.
- `RaccoonWarehouse.Application` contains service-layer logic.
- `RaccoonWarehouse.Data` contains EF Core `ApplicationDbContext`, repositories, and migrations.
- `RaccoonWarehouse.Domain` contains entities and DTOs.
- `RaccoonWarehouse.Core` contains shared interfaces and common abstractions.

## Working Rules
- Start from the solution in `RaccoonWarehouse-master`, not from an individual project unless the task is isolated.
- Preserve the current layering:
  - UI/WPF code stays in `RaccoonWarehouse-master`
  - business logic goes in `RaccoonWarehouse.Application`
  - persistence changes go in `RaccoonWarehouse.Data`
  - entities/DTOs belong in `RaccoonWarehouse.Domain`
  - interfaces/shared contracts belong in `RaccoonWarehouse.Core`
- Follow existing dependency-injection registration in `RaccoonWarehouse-master/App.xaml.cs` when adding services or windows.
- Prefer extending existing feature folders instead of creating new top-level folders.
- Keep changes narrow and consistent with the existing style, even where the codebase is not perfectly clean.

## Build and Verification
- Preferred solution build:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.sln`
- Preferred app build:
  - `dotnet build RaccoonWarehouse-master/RaccoonWarehouse.csproj`
- If a task changes EF Core models or persistence, also inspect `RaccoonWarehouse.Data/Migrations`.
- There do not appear to be dedicated test projects in this repository, so verification will often be build-focused unless tests are added later.

## QA Summary Workflow
- Before starting any new testing or QA-related update, read:
  - `RaccoonWarehouse.Tests/QA_Testing_Summary.md`
- After finishing testing or QA-related updates, update the same summary file with:
  - scope tested
  - pass/fail counts
  - key findings
  - remaining risks/gaps

## QA Testing Roles
- Act as QA when requested and translate scenarios into test cases with:
  - preconditions
  - test steps
  - expected result
  - actual result
  - pass/fail status
- Prioritize CRUD and regression coverage for critical modules (`Products`, `Invoices`, `Stocks`, `POS`).
- Prefer automated tests when feasible:
  - unit tests for service/business logic in `RaccoonWarehouse.Application`
  - integration tests for EF/repository behavior in `RaccoonWarehouse.Data`
  - UI automation tests for key WPF flows when explicitly requested
- For each bug found, provide a reproducible bug report with:
  - title
  - severity (`Critical`, `High`, `Medium`, `Low`)
  - environment/build used
  - exact reproduction steps
  - expected vs actual
  - evidence (logs/test output)
- Always provide a final QA summary:
  - tested scenarios
  - passed/failed counts
  - blocked items
  - risks and recommended next checks
- For each test execution, always do the following:
  - handle `try/catch`
  - handle program crashing and avoid it
  - add loading window where needed
  - input incorrect values and data types

## Data and Environment Notes
- `RaccoonWarehouse.Data/ApplicationDbContext.cs` currently contains a hardcoded SQL Server connection in `OnConfiguring`.
- Treat database-related edits as high risk. Do not silently replace or remove connection behavior without confirming the intended deployment setup.
- `ApplicationDbContextFactory` is present for design-time EF operations.

## UI Notes
- The app uses DI to resolve windows and services on startup.
- Many screens are code-behind heavy WPF windows rather than MVVM-first components. Match the local pattern unless the task explicitly asks for a broader refactor.
- Reports and PDF generation use libraries already referenced in the main app project, including QuestPDF, MigraDocCore, and PdfSharpCore.

## Change Guidance
- For bug fixes, inspect both the WPF window and the corresponding application service before editing.
- For new CRUD features, mirror the existing pattern:
  - domain entity/DTO updates
  - data/repository adjustments if needed
  - application service changes
  - WPF window/XAML integration
  - DI registration in `App.xaml.cs` if adding a new window or service
- For POS, invoice, stock, and report work, check for existing related feature files before introducing new flows.
- UI nullability rule:
  - If a field is nullable in entity/DTO (`?`), the UI must allow null/empty input for that field.
  - Only non-nullable fields should be treated as required in UI validation.

## Avoid
- Do not move projects or restructure the solution unless explicitly requested.
- Do not hardcode additional environment-specific paths or credentials.
- Do not introduce a new architectural pattern across the codebase as part of a small feature or bug fix.
