# ADR-001: Use Blazor WebAssembly for the SVG Editor

## Status

Accepted

## Context

SvgEditor needs a rich, interactive client-side experience for creating and editing SVG graphics
directly in the browser. The application must support real-time canvas manipulation, drag-and-drop,
and responsive UI updates without full page reloads. We need to choose a client-side framework
that aligns with the team's .NET expertise.

## Decision

We will use **Blazor WebAssembly (standalone)** as the client-side application framework,
targeting **.NET 10**.

Key reasons:

- Full C# on the client — no context switching to JavaScript for business logic.
- SVG DOM manipulation and serialisation can use `System.Xml.Linq` natively.
- Single-language stack simplifies testing (MSTest for both domain and UI logic).
- The standalone WASM hosting model allows deployment as static files to any CDN.

## Consequences

- **Easier:** Shared models between future server-side components and the client. C# type safety
  for SVG element models and geometry calculations.
- **Harder:** Initial download size is larger than a pure JS framework. Debugging WebAssembly can
  be more complex. JavaScript interop is still needed for browser-specific APIs
  (File API, `localStorage`, clipboard).
