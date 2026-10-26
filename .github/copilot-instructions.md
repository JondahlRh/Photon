# AI Coding Instructions – Project Photon (Raytracer)

You are an expert C# software engineer assisting in the development of "Photon", a cross-platform 3D raytracer built with .NET.
Whenever generating or refactoring code for this project, you MUST strictly adhere to the rules below.

---

## 1. Language & Naming Conventions

- **Language:** Write ALL code, identifiers, XML documentation, and comments in **English**.
- **Identifiers:**
  - `PascalCase`: Classes, structs, records, enums, methods, properties, public fields, and constants (`const`).
  - `camelCase`: Method parameters and local variables.
  - `_camelCase` (leading underscore): Private and protected instance fields.
  - `IPascalCase` (prefixed with `I`): Interfaces (e.g., `IShape`, `ILightSource`).
- **Self-explaining code:** Use concise, intention-revealing names.

---

## 2. Architecture & Dependencies (Critical)

- **Layered Architecture:** The solution consists of three projects. Dependencies only point downwards:
  - `Photon.App`: Avalonia UI, OpenGL preview (OpenTK), image export (SkiaSharp). The only project allowed to use UI or graphics libraries.
  - `Photon.Application`: View models, services and platform interfaces (e.g., file dialogs, image export).
  - `Photon.Core`: Data model, rendering, and OBJ/MTL import. Must not reference `Photon.Application` or `Photon.App`.
- **GUI-free projects:** `Photon.Core` and `Photon.Application` must be completely decoupled from any UI or graphics framework (Avalonia, OpenTK, SkiaSharp, WPF, etc.).
  - They must compile, run, and be testable headlessly.
  - Never introduce UI-related types (like `Bitmap`, `Window`, `Control`) into these projects.
  - When the application layer needs a platform service, define an interface in `Photon.Application.Platform` and implement it in `Photon.App`.
  - Represent render targets and framebuffers using pure data structures (e.g., raw arrays, `Vector3[,]`, or standard image byte buffers).
- **Standard Libraries:** Prefer standard, hardware-accelerated types from `System.Numerics` (e.g., `Vector3`, `Vector4`, `Matrix4x4`, `Quaternion`) over custom vector implementations.

---

## 3. C# Language Idioms

- **File-scoped Namespaces:** Always use file-scoped namespaces (e.g., `namespace Photon.Core.Rendering;`).
- **Nullable Reference Types:** The project has nullable checks enabled (`<Nullable>enable</Nullable>`).
  - Never ignore nullability warnings.
  - Explicitly mark optional types with `?` and handle null checks cleanly.
- **Type Inference:** Prefer `var` when the type is obvious from the right-hand side of the assignment (e.g., `var ray = new Ray(...);`), or use target-typed `new()` where applicable.

---

## 4. Documentation & Header Rules

### Mandatory File Header

Every newly generated `.cs` file MUST start with the following header:

```csharp
// -----------------------------------------------------------------------------
// Author:        [Author Name]
// AI Assistance: Generated with AI assistance
// -----------------------------------------------------------------------------
```

### Comments & XML Docs

- **No trivial comments:** Do not write comments that merely repeat what the code does (e.g., avoid `// set width`).
- **XML documentation for every element:** The project requirements demand documentation of all namespaces, classes, fields, and methods. Every type and member, including `private` and `internal` ones, must have concise XML documentation:
  - Provide `<summary>`, `<param>`, and `<returns>` tags.
  - Use `<inheritdoc/>` for overrides and interface implementations.
  - Each namespace is described in a `NamespaceDoc.cs` file containing an `internal static class NamespaceDoc` with a `<summary>`.
- **Math & Physics algorithms:** When implementing rendering mathematics (e.g., Möller-Trumbore intersection, Snell's law, Fresnel equations, Phong/Blinn-Phong reflection, coordinate transformations):
  - Include short inline comments explaining the formula, coordinate spaces, or mathematical derivation.

---

## 5. Code Formatting (CSharpier Compliance)

- Format code according to standard CSharpier formatting:
  - 4 spaces for indentation (no tabs).
  - Unix line endings (`LF`).
  - Do not leave trailing whitespaces.
