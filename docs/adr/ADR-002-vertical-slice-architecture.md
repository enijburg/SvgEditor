# ADR-002: Adopt Vertical Slice Architecture

## Status

Accepted

## Context

The project needs a clear code organisation strategy. Traditional layered architecture
(Controllers → Services → Repositories) tends to scatter a single feature across many folders and
creates tight coupling between layers. For a feature-rich editor like SvgEditor, we need each
capability (e.g., add element, undo/redo, import SVG) to be self-contained and easy to navigate.

## Decision

We will use **Vertical Slice Architecture (VSA)** where each feature lives in its own directory
under `src/SvgEditor.Web/Features/`. A slice owns its command/query, handler, validator, and UI
component.

Folder structure:

```
src/SvgEditor.Web/
├── Features/
│   ├── Canvas/          ← element CRUD, selection, reorder
│   ├── History/         ← undo/redo
│   ├── Styling/         ← fill, stroke, font
│   ├── IO/              ← import/export SVG files
│   └── Layers/          ← layer management
└── Shared/              ← cross-cutting: mediator, validation, storage
```

Rules:
- No feature may import code from another feature's internal folder.
- Cross-feature shared types belong in `Shared/` or `Features/*/Models/`.
- One handler per slice; handlers must not call other handlers directly.

## Consequences

- **Easier:** Adding a new feature is a single self-contained folder. Discoverability is high —
  find the feature name, find all its code. Deleting a feature is straightforward.
- **Harder:** Some duplication between slices (e.g., similar validation patterns). Requires
  discipline to keep shared code truly shared and not leak feature internals.
