# agents.md – SvgEditor Development Principles

This document anchors the architectural principles and coding conventions for all contributors
(human or AI) working on the **SvgEditor** codebase. Any automated agent must adhere to these
rules before proposing or committing changes.

---

## 1. Project Purpose

**SvgEditor** is a .NET 10 Blazor WebAssembly application for creating and editing SVG graphics
in the browser. Core capabilities include:

- Interactive canvas with element selection, move, resize, and rotate
- Shape primitives: rect, circle, ellipse, line, polyline, polygon, path, text
- Style panel: fill, stroke, opacity, font properties
- Layer management (z-order, show/hide, lock)
- Undo/redo history
- Import/export of `.svg` files via the browser File API
- Document state persisted to `localStorage` so work survives page reloads

---

## 2. Architecture: Vertical Slice Architecture (VSA)

Each feature lives in its own directory under `src/SvgEditor.Web/Features/`.  
A "slice" owns everything it needs: command/query, handler, page/component, and validator.  
Shared cross-cutting code lives in `src/SvgEditor.Web/Shared/`.

```
src/SvgEditor.Web/
├── Features/
│   ├── Canvas/
│   │   ├── Models/            ← SvgDocument, SvgElement, BoundingBox, …
│   │   ├── AddElement/        ← command + handler
│   │   ├── UpdateElement/
│   │   ├── DeleteElement/
│   │   ├── SelectElement/
│   │   ├── ReorderElements/   ← layer z-order
│   │   └── CanvasPage/        ← .razor page + supporting components
│   ├── History/
│   │   ├── Models/            ← HistoryEntry, HistoryStack
│   │   ├── PushHistory/
│   │   └── UndoRedo/
│   ├── Styling/
│   │   ├── Models/            ← FillStyle, StrokeStyle, FontStyle
│   │   ├── UpdateStyle/
│   │   └── StylePanel/        ← .razor component
│   ├── IO/
│   │   ├── ImportSvg/         ← parse SVG file → SvgDocument
│   │   └── ExportSvg/         ← serialise SvgDocument → .svg download
│   └── Layers/
│       ├── Models/            ← Layer
│       ├── AddLayer/
│       ├── DeleteLayer/
│       └── LayersPanel/       ← .razor component
├── Shared/
│   ├── Mediator/              ← lightweight custom IMediator
│   ├── Validation/            ← lightweight custom IValidator
│   └── Storage/               ← browser storage abstraction (IStorageService)
└── Pages/                     ← top-level routing pages
```

**Rules:**
- No feature may import code from another feature's internal folder.
- Cross-feature shared types belong in `Shared/` or `Features/*/Models/`.
- One handler per slice; handlers must not call other handlers directly.
- All canvas mutations must go through the `History` feature so undo/redo remains consistent.

---

## 3. No Commercial Packages

**Do not add** MediatR, FluentValidation, AutoMapper, or any other commercial/paid abstraction
packages. Instead use:

| Need                   | Solution                                            |
|------------------------|-----------------------------------------------------|
| Request dispatching    | `SvgEditor.Web.Shared.Mediator` (custom IMediator)  |
| Validation             | `SvgEditor.Web.Shared.Validation` (custom IValidator)|
| Object mapping         | Explicit constructor / `init` property assignment   |
| Logging                | `ILogger<T>` from `Microsoft.Extensions.Logging`    |
| SVG serialisation      | `System.Xml` / `System.Xml.Linq` (built-in)         |

When .NET natively supports a pattern, prefer it over a custom implementation.

---

## 4. SVG Domain Rules

- `SvgDocument` is the root model; it owns an ordered list of `SvgElement` objects.
- `SvgElement` is an abstract base; concrete types are `SvgRect`, `SvgCircle`, `SvgPath`, `SvgText`, etc.
- All coordinates are in SVG user units (no unit suffix). Conversions to/from screen pixels happen only in the canvas component.
- IDs are stable `string` GUIDs assigned at creation and never changed — do not use array indices as identity.
- SVG export must produce well-formed XML that validates against the SVG 1.1 schema.

---

## 5. Testing

- Test framework: **MSTest 4.x** (latest stable). Use `[TestClass]` / `[TestMethod]`.
- Assertions: **MSTest `Assert.*`** exclusively — `Assert.AreEqual`, `Assert.IsNotNull`,
  `Assert.IsTrue`, `CollectionAssert.*`, `Assert.ThrowsExactlyAsync<T>`, etc.
- Do **not** use xUnit, NUnit, Shouldly, FluentAssertions, or any third-party assertion library.
- Tests live under `tst/SvgEditor.Web.Tests/` mirroring the `src/` structure.
- UI-independent logic (handlers, serialisation, geometry calculations) must have unit tests.
- Use `InMemoryStorageService` (in `tst/…/Fakes/`) to isolate handler tests from the browser.

---

## 6. Storage Strategy

Current implementation: `BrowserStorageService` via JS interop → `localStorage`.

The active `SvgDocument` is serialised as JSON and stored under a well-known key defined in
`StorageKeys.cs`. File import/export uses the browser File API via JS interop — not `localStorage`.

Future: replace `BrowserStorageService` with a server-syncing implementation that implements
the same `IStorageService` interface. No other code should need to change.

---

## 7. Code Style

- Language: C# 13 / .NET 10. Enable `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- Use `sealed` on non-inheritable classes.
- Use primary constructors for DI injection (`SomeHandler(IStorageService storage)`).
- Records / `init`-only properties for commands, queries, and model snapshots.
- Keep Razor components thin: delegate all data access and mutations to handlers via `IMediator`.
- No business logic in `.razor` code blocks — geometry, hit-testing, and serialisation live in handlers or domain types.
- Alsways prefer LoggerMessage code generator if logging is added
- Enable errors on warnings

---

## 8. Project Layout

```
SvgEditor/
├── src/
│   └── SvgEditor.Web/          ← Blazor WASM application
├── tst/
│   └── SvgEditor.Web.Tests/    ← MSTest unit tests
├── docs/
│   └── adr/                    ← Architecture Decision Records
├── agents.md                   ← this file
├── SvgEditor.sln
└── .gitignore
```

---

## 9. No Sample / Template Code

The project must not contain default Blazor template boilerplate. When scaffolding or resetting
the project, remove the following immediately:

- **Sample pages**: `Counter.razor`, `Weather.razor`, `Home.razor` (in `Pages/`)
- **Sample data**: `wwwroot/sample-data/` directory (e.g. `weather.json`)
- **Placeholder tests**: `Test1.cs` or any empty test stubs
- **Template nav links**: remove Counter, Weather, and any other sample entries from `NavMenu.razor`
- **Template chrome**: remove the "About" link in `MainLayout.razor` top row

Only application-specific pages, components, and test files should exist in the repository.

---

## 10. Architecture Decision Records

Every significant architectural decision must have a corresponding ADR in `docs/adr/`.  
Format: `ADR-NNN-short-title.md`. Template in `docs/adr/ADR-000-template.md`.

---

## 11. Contribution Checklist (for agents)

Before submitting any change:

- [ ] All existing tests pass (`dotnet test`).
- [ ] New behaviour has corresponding MSTest unit tests with `Assert.*` assertions.
- [ ] No new commercial packages introduced.
- [ ] Vertical slice boundaries respected (no cross-feature imports).
- [ ] All canvas mutations route through the `History` feature.
- [ ] SVG export produces well-formed XML.
- [ ] `agents.md` updated if architectural rules change.
- [ ] Relevant ADR created or updated in `docs/adr/` if a significant decision was made.
