# Solution Structure

This document describes the `dotnet` CLI commands used to create the SvgEditor solution
structure and their purpose.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0 or later)

## Commands Used

### 1. Create the Solution File

```bash
dotnet new sln --name SvgEditor
```

Creates an `SvgEditor.slnx` solution file in the repository root. .NET 10 defaults to the
new XML-based `.slnx` format which is simpler to read and produces cleaner diffs than the
legacy `.sln` format.

### 2. Create the Blazor WebAssembly Project

```bash
dotnet new blazorwasm --name SvgEditor.Web --output src/SvgEditor.Web --framework net10.0
```

Scaffolds a standalone Blazor WebAssembly application under `src/SvgEditor.Web/` targeting
.NET 10. This is the main web application project.

### 3. Create the MSTest Unit Test Project

```bash
dotnet new mstest --name SvgEditor.Web.Tests --output tst/SvgEditor.Web.Tests --framework net10.0
```

Creates an MSTest 4.x test project under `tst/SvgEditor.Web.Tests/`. MSTest is used as the
sole testing framework (see [ADR-004](docs/adr/ADR-004-mstest-for-testing.md)).

### 4. Add Projects to the Solution

```bash
dotnet sln SvgEditor.slnx add src/SvgEditor.Web/SvgEditor.Web.csproj
dotnet sln SvgEditor.slnx add tst/SvgEditor.Web.Tests/SvgEditor.Web.Tests.csproj
```

Registers both projects in the solution file. The `dotnet sln add` command automatically
organises projects into solution folders matching their directory structure (`src/` and `tst/`).

### 5. Add Project Reference from Tests to Web Project

```bash
dotnet add tst/SvgEditor.Web.Tests/SvgEditor.Web.Tests.csproj reference src/SvgEditor.Web/SvgEditor.Web.csproj
```

Adds a project reference so the test project can access types from the main application
project for unit testing.

## Project Configuration

Both projects are configured with the following settings in their `.csproj` files:

| Setting                       | Value      | Purpose                                    |
|-------------------------------|------------|--------------------------------------------|
| `TargetFramework`             | `net10.0`  | Target .NET 10                             |
| `LangVersion`                 | `latest`   | Use the latest stable C# language version  |
| `Nullable`                    | `enable`   | Enable nullable reference type analysis    |
| `ImplicitUsings`              | `enable`   | Enable implicit global using directives    |
| `TreatWarningsAsErrors`       | `true`     | Promote all warnings to errors             |

## Folder Layout

```
SvgEditor/
├── src/
│   └── SvgEditor.Web/                  ← Blazor WASM application
│       ├── Features/
│       │   ├── Canvas/
│       │   │   ├── Models/             ← SvgDocument, SvgElement, BoundingBox
│       │   │   ├── AddElement/         ← command + handler
│       │   │   ├── UpdateElement/
│       │   │   ├── DeleteElement/
│       │   │   ├── SelectElement/
│       │   │   ├── ReorderElements/    ← layer z-order
│       │   │   └── CanvasPage/         ← .razor page + components
│       │   ├── History/
│       │   │   ├── Models/             ← HistoryEntry, HistoryStack
│       │   │   ├── PushHistory/
│       │   │   └── UndoRedo/
│       │   ├── Styling/
│       │   │   ├── Models/             ← FillStyle, StrokeStyle, FontStyle
│       │   │   ├── UpdateStyle/
│       │   │   └── StylePanel/         ← .razor component
│       │   ├── IO/
│       │   │   ├── ImportSvg/          ← parse SVG file → SvgDocument
│       │   │   └── ExportSvg/          ← serialise SvgDocument → .svg download
│       │   └── Layers/
│       │       ├── Models/             ← Layer
│       │       ├── AddLayer/
│       │       ├── DeleteLayer/
│       │       └── LayersPanel/        ← .razor component
│       └── Shared/
│           ├── Mediator/               ← custom IMediator (no MediatR)
│           ├── Validation/             ← custom IValidator (no FluentValidation)
│           └── Storage/                ← browser storage abstraction
├── tst/
│   └── SvgEditor.Web.Tests/            ← MSTest unit tests
├── docs/
│   └── adr/                            ← Architecture Decision Records
├── agents.md                           ← development principles
├── SolutionStructure.md                ← this file
├── SvgEditor.slnx                      ← solution file
└── .gitignore
```

## Common Developer Commands

```bash
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Run all tests
dotnet test

# Run the Blazor WASM application
dotnet run --project src/SvgEditor.Web
```

## Architecture Decision Records

Major architectural decisions are documented in `docs/adr/`:

| ADR | Title |
|-----|-------|
| [ADR-001](docs/adr/ADR-001-use-blazor-wasm.md) | Use Blazor WebAssembly for the SVG Editor |
| [ADR-002](docs/adr/ADR-002-vertical-slice-architecture.md) | Adopt Vertical Slice Architecture |
| [ADR-003](docs/adr/ADR-003-no-commercial-packages.md) | No Commercial or Paid Abstraction Packages |
| [ADR-004](docs/adr/ADR-004-mstest-for-testing.md) | Use MSTest as the Test Framework |
| [ADR-005](docs/adr/ADR-005-solution-structure.md) | Solution and Project Structure |
