---
apply: always
---

# General Code Review Guidelines

## Project Dependency Rules:
- The engine can only depend on the SDK and other engine modules.  The engine's role is to implement the game systems defined in the SDK
- The game client can only depend on the SDK.  The client's role is to provide a user interface into game systems provided by the engine.
- The SDK defines all shared interfaces and data structures

## Naming
- Use clear, descriptive names for variables, functions, and classes
- Avoid single-letter names except for loop indices
- Follow consistent naming conventions throughout the project

## Style
- Keep line length reasonable (e.g., 100–120 characters)
- Use consistent indentation and spacing
- Include comments for complex logic or important decisions
- DO NOT USE `!` TO SUPPRESS NULL WARNINGS!!!
- Use primary constructors where they simplify dependency assignment
- Use file-scoped namespaces to reduce indentation levels
- ALWAYS use `var`
- NEVER use syntax like this to get around reserved words `string @keyword`.

## Structure
- Keep functions short and focused on a single responsibility
- Avoid deep nesting and long parameter lists
- Group related code logically
- Use constructor injection for dependencies (e.g., `ILoggerFactory`, `IWindow`)
- Prefer asynchronous methods for long-running operations (e.g., world generation, resource loading)

## Memory Management & Resource Lifecycle
- Implement `IDisposable` for classes holding unmanaged resources (e.g., OpenGL textures)
- Use the `Dispose(bool)` pattern when necessary
- Explicitly delete OpenGL resources (e.g., `gl.DeleteTexture`) during disposal
- Use finalizers (`~ClassName()`) in classes managing raw OpenGL handles to ensure cleanup

## Testing
- Critical: Use TDD for new features and bug fixes
- Use XUnit, AwesomeAssertions, and Bogus
- Tests only verify one unit of behavior
- Tests verify requirements/behavior, not implementation
- Use manual mocks or fakes to isolate units when dependencies are complex
- Use `NullLogger<T>.Instance` when testing classes that require an `ILogger`
- Consistently use `Should()` from AwesomeAssertions for readable verifications

## Best Practices
- Avoid duplicate code
- Prefer composition over inheritance
- Handle errors and edge cases gracefully
- Use SOLID principles
- Limit `unsafe` blocks to direct API interactions (e.g., passing pointers to Silk.NET/OpenGL)

## Logging
- Use high-performance logging (the `[LoggerMessage]` attribute) for log events to improve performance
- Include logging for state changes when adding new features (e.g., rendering toggles)

## Documentation
- Write XMLDoc comments for public members and types
- Keep documentation up to date with code changes

## Tools
- Follow project-specific tooling or linters
- Use version control best practices (e.g., atomic commits, meaningful messages)
- You may use `git commit` to submit small change sets if:
  - There are no compiler errors or warnings
  - All tests pass