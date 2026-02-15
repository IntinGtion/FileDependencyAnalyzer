# FileDependencyAnalyzer

[![CI](https://github.com/IntinGtion/FileDependencyAnalyzer/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/IntinGtion/FileDependencyAnalyzer/actions/workflows/ci.yml)

A small, rule-based CLI tool that scans a local folder tree and builds a **directed dependency graph** between files.

- **Directed edge:** `A -> B` means file **A references/depends on** file **B**
- Every successfully read file becomes a node (even if it has no dependencies)

## Features

- 🔍 Recursively scans a folder and builds a dependency graph (nodes + edges)
- 🧩 Extensible rule system (`IDependencyRule`)
- 📊 Summary output:
  - node/edge counts
  - top inbound/outbound files
  - orphan files
  - detected cycles
- 📦 Export formats:
  - `summary` (human-readable)
  - `json` (structured export)
  - `md` (GitHub-friendly report)

## Supported Rules

- **Markdown**
  - Detects relative links in `[text](path)`
  - Ignores external `http*` links
- **PowerShell**
  - Detects dot-sourcing (`. .\helper.ps1`)
  - Detects `Import-Module` paths
  - Supports simple `$PSScriptRoot` replacement; skips unresolved variables

## Requirements

- .NET 8 SDK

## Build & Test

```bash
dotnet build
dotnet test
```

## Run

### Quickstart (summary to console)

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- scan .
```

Shorthand also works:

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- .
```

### Markdown report (file)

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- scan . --format md --out dependency-report.md
```

### JSON export (file)

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- scan "D:\Projects\MyRepo" --format json --out graph.json
```

## Excluding directories

By default, these directory names are excluded: `.git`, `.vs`, `bin`, `obj`

Add more via `--exclude-dir` (repeatable):

```bash
dotnet run --project src/FileDependencyAnalyzer.Cli -- scan . --exclude-dir node_modules --exclude-dir dist
```

## CLI Options

```text
FileDependencyAnalyzer <path>
FileDependencyAnalyzer scan <path> [options]
FileDependencyAnalyzer scan [options] <path>

Options:
  --format summary|json|md   Output format (default: summary)
  --out <file>              Write output to file (default: stdout)
  --top <n>                 Top inbound/outbound list size (default: 5)
  --max-orphans <n>         Max orphan files listed (default: 20)
  --max-cycles <n>          Max cycles listed (default: 20)
  --exclude-dir <name>      Exclude directory name (repeatable)
  --full-paths              Print full paths instead of relative paths
  -h|--help                 Show help
```

Tip: run `FileDependencyAnalyzer --help` to see the built-in usage text.

## Extending

Add a new rule by implementing `IDependencyRule` in `FileDependencyAnalyzer.Core.Rules`
and register it in the CLI (`Program.cs`) when creating the `FileScanner`.

## Roadmap

- Include / Exclude glob patterns (not only directory-name excludes)
- Graph exports (e.g. Mermaid / GraphViz DOT)
- Package as a `dotnet tool` for global install