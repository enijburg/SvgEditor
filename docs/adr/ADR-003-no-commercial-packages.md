# ADR-003: No Commercial or Paid Abstraction Packages

## Status

Accepted

## Context

Popular .NET packages such as MediatR, FluentValidation, and AutoMapper have moved to
commercial licensing models. Using them introduces licensing risk and cost. Additionally,
lightweight custom implementations are sufficient for the scope of SvgEditor.

## Decision

We will **not** use MediatR, FluentValidation, AutoMapper, or any other commercial/paid
abstraction packages. Instead:

| Need                 | Solution                                            |
|----------------------|-----------------------------------------------------|
| Request dispatching  | Custom `IMediator` in `Shared/Mediator/`            |
| Validation           | Custom `IValidator` in `Shared/Validation/`         |
| Object mapping       | Explicit constructors / `init` property assignment  |
| Logging              | `ILogger<T>` from `Microsoft.Extensions.Logging`    |
| SVG serialisation    | `System.Xml` / `System.Xml.Linq` (built-in)        |

## Consequences

- **Easier:** No licensing concerns. Full control over abstractions. Smaller dependency footprint
  and faster builds.
- **Harder:** Initial effort to implement and test custom mediator and validator. Fewer
  community examples to reference compared to MediatR/FluentValidation patterns.
