# ADR-004: Use MSTest as the Test Framework

## Status

Accepted

## Context

We need a unit testing framework for the project. The main candidates are xUnit, NUnit, and
MSTest. All three are mature and well-supported in the .NET ecosystem.

## Decision

We will use **MSTest 4.x** (latest stable) as the sole test framework.

- Use `[TestClass]` / `[TestMethod]` attributes.
- Use `Assert.*` assertions exclusively (`Assert.AreEqual`, `Assert.IsNotNull`,
  `Assert.IsTrue`, `CollectionAssert.*`, `Assert.ThrowsExactlyAsync<T>`, etc.).
- Do **not** use xUnit, NUnit, Shouldly, FluentAssertions, or any third-party assertion library.

Test project location: `tst/SvgEditor.Web.Tests/`, mirroring the `src/` structure.

## Consequences

- **Easier:** MSTest is a first-party Microsoft framework, well-integrated with Visual Studio and
  `dotnet test`. MSTest 4.x has modern features including source-generated test discovery.
- **Harder:** Developers familiar with xUnit or NUnit conventions will need to adjust.
  MSTest assertions are less fluent than FluentAssertions but are sufficient for our needs.
