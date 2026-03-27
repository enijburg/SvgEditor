# ADR-005: Solution and Project Structure

## Status

Accepted

## Context

The project needs a consistent and well-defined solution structure that organises source code,
tests, and documentation. The structure must support the Vertical Slice Architecture and be
easy to navigate for both human developers and automated agents.

## Decision

We will use the following layout created entirely with `dotnet` CLI commands:

```
SvgEditor/
├── src/
│   └── SvgEditor.Web/          ← Blazor WASM application (.NET 10)
├── tst/
│   └── SvgEditor.Web.Tests/    ← MSTest unit tests
├── docs/
│   └── adr/                    ← Architecture Decision Records
├── agents.md
├── SvgEditor.slnx              ← XML-based solution file (.NET 10 default)
└── .gitignore
```

Key decisions:

- **`.slnx` format:** .NET 10 defaults to the new XML-based `.slnx` solution file format.
  We adopt this modern format over the legacy `.sln` format.
- **`src/` and `tst/` separation:** Source and test projects live in separate top-level
  directories for clear separation of concerns. Solution folders mirror this structure.
- **Warnings as errors:** Both projects enable `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  to enforce code quality from the start.
- **Feature folder structure:** Empty directories under `Features/` and `Shared/` are preserved
  via `.gitkeep` files, establishing the VSA skeleton before any code is written.

All commands and their purposes are documented in `SolutionStructure.md`.

## Consequences

- **Easier:** New contributors can immediately understand the project layout. The `dotnet` CLI
  commands are reproducible and documented. The `.slnx` format is simpler to read and merge.
- **Harder:** Some older tooling may not fully support `.slnx` yet. The empty folder structure
  with `.gitkeep` files adds minor clutter until actual code files are added.
