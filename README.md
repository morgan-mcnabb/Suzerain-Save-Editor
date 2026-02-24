# Suzerain Save Editor

A desktop save editor for [Suzerain](https://store.steampowered.com/app/1207650/Suzerain/) that lets you open your save files, tweak variables, and write changes back to disk without breaking anything.

The editor understands the game's save format (JSON wrapper around Lua variable tables) and does byte-perfect round-tripping, so everything it doesn't touch stays exactly as it was. Automatic backups are created every time you save, just in case.

## Features

- **Tabbed editing** for General, Sordland, and Rizia variables with search and filtering
- **Advanced tab** with hierarchical tree navigation for browsing all 12,000+ variables
- **Live validation** with inline error feedback (type checks, min/max bounds, enum options)
- **Automatic backups** on every save to a `backups/` folder next to the save file
- **Atomic writes** so a crash mid-save won't corrupt your file
- **Dark theme** UI

## Requirements

- Windows 10 or later
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (if using the framework-dependent build)

Self-contained builds include the runtime and don't need anything extra installed.

## Getting Started

### Download

Grab the latest release from the [Releases](https://github.com/morgan-mcnabb/Suzerain-Save-Editor/releases) page.

### Building from source

```bash
git clone https://github.com/morgan-mcnabb/Suzerain-Save-Editor.git
cd Suzerain-Save-Editor
dotnet build
```

To publish a self-contained single-file executable:

```bash
dotnet publish SuzerainSaveEditor.App -p:PublishProfile=win-x64
```

The output will be in `SuzerainSaveEditor.App/bin/publish/win-x64/`.

### Running

1. Launch the application
2. Click **Open** and navigate to your Suzerain save files (the editor will suggest the default save location)
3. Edit whatever you want -- changes are highlighted so you can see what you've modified
4. Click **Save** when you're done

Save files are typically located at:
```
%LOCALAPPDATA%Low\Torpor Games\Suzerain
```

## User Guide

See the [User Guide](USER_GUIDE.md) for a step-by-step walkthrough of using the editor.

## How it works

Suzerain save files are JSON with 14 top-level keys. The `variables` field contains a Lua table serialized as a string with thousands of game state variables. The `entityUpdates` array holds entity field changes.

The editor parses both layers, maps known fields to a human-readable schema, and lets you edit values through a form-based UI. Unknown fields and formatting are preserved during round-tripping.

> **Note:** Windows SmartScreen may show a warning since the executable is not code-signed.
> Click **"More info"** → **"Run anyway"** to proceed. The source code is fully available for review.

## License

[MIT](LICENSE)
