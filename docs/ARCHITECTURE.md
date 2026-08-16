# Architecture

Last verified: 2026-06-13

## Overview

ROCCOPOS is a .NET 8 WPF application organized as a layered solution. The UI is
mostly code-behind oriented, application services contain business workflows,
EF Core provides persistence, and the domain project contains shared entities
and DTOs.

## Project Dependencies

```text
RaccoonWarehouse-master (WPF)
  -> Application
  -> Core
  -> Data
  -> Domain
  -> Modules.Reports

Modules.Reports
  -> Application
  -> Core

Application
  -> Data
  -> Domain

Data
  -> Core
  -> Domain

Core
  -> Domain
```

The current dependency graph is practical but not a strict clean architecture:
the application layer directly references the data project and EF context.

## Startup And Navigation

`RaccoonWarehouse-master/App.xaml.cs` is the composition root. Startup:

1. Registers global exception and localization hooks.
2. Builds the dependency-injection container.
3. Initializes localization and runtime database setup.
4. Displays a loading window.
5. Opens login.
6. Resolves and displays the dashboard after successful authentication.

Services are primarily scoped, windows transient, and session/navigation/
localization/notification helpers singleton. Dashboard modules and action
handlers provide part of the feature navigation, while some screens still use
direct window construction or resolution.

## Layers

### Presentation

`RaccoonWarehouse-master` contains WPF XAML and code-behind, startup wiring,
navigation, PDF/export helpers, localization, and feature folders. Match this
local style unless a task explicitly requests a broader MVVM refactor.

### Application

`RaccoonWarehouse.Application/Service` contains CRUD and workflow services.
Many services inherit a generic service and directly use `ApplicationDbContext`
and `IUOW`. Complex workflows such as POS and external orders coordinate
several services from the UI or one application service.

### Core

`RaccoonWarehouse.Core` contains shared contracts such as user session,
navigation, modules, localization, loading, dialogs, and common abstractions.

### Data

`RaccoonWarehouse.Data` contains `ApplicationDbContext`, repositories,
configurations, connection resolution, and migrations. EF Core registers most
`BaseEntity` subclasses automatically, with explicit indexes and relationships
for important models.

### Domain

`RaccoonWarehouse.Domain` contains entities, DTOs, enums, report filters/results,
and feature-specific models.

## Persistence

- Database: SQL Server through EF Core 9.
- Runtime connection precedence:
  1. `RACCOONWAREHOUSE_CONNECTION_STRING`
  2. deployed `appsettings.json`
  3. source fallback connection string
- Design-time context uses the same provider.
- Soft deletion exists for selected entities.
- Startup includes schema-compatibility routines in addition to EF migrations.

Security risk: credentials currently exist in source-controlled files. Future
work should move secrets to environment or secure deployment configuration and
remove source fallbacks after deployment requirements are confirmed.

## Localization

Arabic is the source language for much existing UI text. English translations
are loaded from `RaccoonWarehouse-master/Localization/LiteralStrings.en.json`.
`UiText` applies translations to windows and supports runtime translation calls.
The selected language is stored in database settings and sets the current
thread culture.

New user-visible text must participate in this shared flow. Logic must not use
translated text as an identifier.

## Reporting

Reports exist in the main WPF project and `RaccoonWarehouse.Modules.Reports`.
Generation/export uses QuestPDF, MigraDocCore, PdfSharpCore, ClosedXML, and
shared report helpers. Report visibility is permission-aware for catalog-based
navigation, but export and non-catalog enforcement require further review.

## External Integrations

### Box

Configured through `appsettings.json`. A singleton HTTP client service reads
pending carts and writes statuses. Import and local status orchestration are
scoped services. No API authentication configuration was found.

### Falcon

A temporary stock import service reconciles remote quantities with local lots.
Its current endpoint/authentication posture should be reviewed before production use.

## Testing

`RaccoonWarehouse.Tests` is an xUnit project using EF Core InMemory. It includes
service tests plus Markdown QA reports. Automated coverage is strongest around
stock, permissions, accounting, and Box order status logic; full WPF/POS
end-to-end automation is limited.

## Architectural Risks

- Several workflows commit documents, inventory, financial transactions, and
  journals in separate operations without one transaction boundary.
- Application-level duplicate checks may be concurrency-sensitive without
  corresponding database uniqueness constraints.
- Permission catalog entries are not consistently enforced at every entry point.
- Plaintext passwords and exposed credentials are critical security debt.
- Startup schema patches and migrations can drift if both are not maintained.
- Multi-warehouse fields exist, but warehouse scoping is not consistently
  propagated through inventory calculations.

## Agent Reading Strategy

For a task, read:

1. `AGENTS.md`
2. `docs/FEATURE_MAP.md`
3. The relevant `docs/features/*.md`
4. Only the listed UI, service, domain, data, and test files

Always verify documentation against the exact source paths being changed.

