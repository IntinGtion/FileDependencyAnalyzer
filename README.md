# FileDependencyAnalyzer

A small, rule-based dependency scanner for local file trees.

It recursively scans a folder and builds a **directed dependency graph** between files.
Current rules:

- **Markdown**: detects relative links in `[text](path)` (external `http*` links are ignored)
- **PowerShell**: detects **dot-sourcing** (`. .\helper.ps1`) and `Import-Module` paths  
  (supports simple `$PSScriptRoot` replacement; skips unresolved variables)

The CLI prints:
- node/edge counts
- top inbound/outbound files
- orphan files
- detected cycles
- the full graph as **JSON** to stdout

## Requirements

- .NET 8 SDK

## Build & Test

```bash
dotnet build
dotnet test
```

## Run (CLI)

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- <path-to-scan>
```

Example:

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- "D:\Projects\MyRepo"
```

## Extending

Add a new rule by implementing `IDependencyRule` in `FileDependencyAnalyzer.Core.Rules`
and register it in the CLI (`Program.cs`) when creating the `FileScanner`.
