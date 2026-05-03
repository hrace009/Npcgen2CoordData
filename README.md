# Npcgen2CoordData

A small Windows Forms utility that imports mob/resource spawn coordinates from a Perfect World style
`npcgen.data` binary file and merges them into a tab-separated `coord_data.txt` file used by related
server tooling.

## Overview

The tool offers three actions through a single form:

- **Load coord_data** — read an existing `coord_data.txt` into memory.
- **Import npcgen.data** — pick a server *maps root* folder (via `FolderBrowserDialog`); the
  tool scans every immediate subfolder for an `npcgen.data` file, parses each one, and merges
  mob (`NpcMobList`) and resource (`ResourcesList`) spawn positions into the loaded coord data,
  keyed by entity id. The map / location name is taken from the subfolder name
  (e.g. `world`, `a78`, `a64`). Empty/truncated files and parse errors are skipped and listed
  in the final status text.
- **Save coord_data** — write the merged result back out as `coord_data.txt`.

Long-running file I/O runs on background threads; progress is reported through the form's
progress bar and status label.

## Stack

- **Language:** C# 7.3
- **Framework:** .NET Framework 3.5 (`v3.5`, WinForms)
- **Output type:** `WinExe` (`AnyCPU`)
- **Project format:** legacy (non-SDK) MSBuild `.csproj`, `ToolsVersion="15.0"`
- **UI:** Windows Forms (Classic). A `FolderBrowserDialog` is used to pick the maps root for batch import.
- **Package manager:** none (only GAC / framework references; no NuGet packages)
- **Solution:** `Npcgen2CoordData.sln`

## Requirements

- Windows with the **.NET Framework 3.5 SP1** targeting pack installed
  (Windows feature: *".NET Framework 3.5 (includes .NET 2.0 and 3.0)"*). Without it MSBuild
  cannot resolve `mscorlib` / `System.Windows.Forms` reference assemblies.
- **MSBuild** shipped with Visual Studio 2017+ or JetBrains Rider (uses the Roslyn `csc.exe`).
  Do **not** build with the legacy `C:\Windows\Microsoft.NET\Framework\v3.5\csc.exe` — it is C# 3.0
  only and will fail on the string interpolation / `?.` syntax used in the codebase.
- `Microsoft.VisualBasic` is still listed in `<ItemGroup>` but is **currently unused** by the
  source (the previous `InputBox` prompt was replaced with a `FolderBrowserDialog`). The
  reference is kept to avoid churning the legacy csproj; it can be removed safely if desired.

## Build

From the repository root in PowerShell:

```powershell
& "C:\Program Files\Microsoft Visual Studio\<edition>\MSBuild\Current\Bin\MSBuild.exe" `
    Npcgen2CoordData\Npcgen2CoordData.csproj /v:m /nologo
```

Replace `<edition>` with your installed VS edition (e.g. `2022\Community`). Rider users can simply
open `Npcgen2CoordData.sln` and build.

Build output:

- `Npcgen2CoordData\bin\Debug\Npcgen2CoordData.exe`
- `Npcgen2CoordData\bin\Release\Npcgen2CoordData.exe`

## Run

Launch the built executable directly:

```powershell
.\Npcgen2CoordData\bin\Debug\Npcgen2CoordData.exe
```

Typical workflow:

1. Click **Load coord_data** and pick an existing `coord_data.txt` (optional — start empty otherwise).
2. Click **Import npcgen.data** and select the *maps root* folder, i.e. the directory whose
   immediate subfolders are individual maps containing an `npcgen.data` file (e.g.
   `.../maps/world/npcgen.data`, `.../maps/a78/npcgen.data`). Each subfolder is processed in
   one pass; the subfolder name becomes the map name in `coord_data.txt`.
3. Click **Save coord_data** to write the merged output.

### Entry point

- `Npcgen2CoordData/Program.cs` → `Program.Main` → `Application.Run(new Form1())`
- UI: `Npcgen2CoordData/Form1.cs` (+ `Form1.Designer.cs`, `Form1.resx`)

## Scripts

This repository ships **no build/run scripts, no `package.json`, no CI configuration**. The
PowerShell command shown under [Build](#build) is the canonical way to compile from the command
line.

## Environment variables

None are read by the application.

## Tests

There is **no test project, no test framework reference, and no CI** in this repository.

Ad-hoc tests are written as standalone console programs that compile against the production `.cs`
files directly. See `.junie/AGENTS.md` §2 for a verified end-to-end recipe (including a
`CoordData` round-trip example). High-level steps:

1. Create a `.cs` file at the repo root with `static int Main()` in `namespace Npcgen2CoordData`.
2. Re-declare the four progress delegates (`SetProgressMax/Next/Value/Text`) in the test file
   (they live in `Form1.cs`, which must not be pulled into the test build).
3. Compile with the Roslyn `csc.exe` from VS/Rider's MSBuild folder (`/langversion:7.3`,
   `/target:exe`) and include the production source files you exercise
   (e.g. `Npcgen2CoordData\CoordData.cs`).
4. Run the produced `.exe` and check `$LASTEXITCODE`.
5. Delete the test source/binary afterwards so they do not leak into the project tree.

> Note: at least `ProgressMax` (and, for `CoordData.Read`, `ProgressText`) is invoked
> unconditionally without `?.` — tests must subscribe to those events or they will throw
> `NullReferenceException`.

## Project structure

```
Npcgen2CoordData.sln                  Visual Studio solution
LICENSE                               GNU GPL v3
README.md                             This file
.junie/AGENTS.md                      Contributor notes (build, test recipe, gotchas)
Npcgen2CoordData/
├── Npcgen2CoordData.csproj           Legacy-format csproj, net35, WinExe
├── Program.cs                        Application entry point
├── Form1.cs                          Main form + progress delegate types
├── Form1.Designer.cs                 Generated designer code
├── Form1.resx                        Form resources
├── CoordData.cs                      coord_data.txt reader/writer
├── npcgen.cs                         npcgen.data binary reader (versioned format)
├── icon.ico                          App icon (referenced as ApplicationIcon and Content)
└── Properties/
    ├── AssemblyInfo.cs
    ├── Resources.resx / .Designer.cs (present, currently unused)
    └── Settings.settings / .Designer.cs (present, currently unused)
```

## Known gotchas

A few items worth knowing before changing code (see `.junie/AGENTS.md` for the full list):

- **Culture-sensitive parsing.** `CoordData.Read`/`Save` assume the current thread culture uses
  `,` as the decimal separator (`Replace('.', ',')` on read, `Replace(",", ".")` on save). On
  `en-US` machines this throws `FormatException`. The proper fix is `CultureInfo.InvariantCulture`.
- **Threading.** `Form1` sets `CheckForIllegalCrossThreadCalls = false` and updates UI from worker
  threads directly. Do not introduce `Invoke`/`async`-`await` without auditing the whole flow.
- **Binary format versioning.** `NpcGen.ReadNpcgen` branches on `Version > 6`, `> 9`, `>= 11`.
  Mirror any additions in the corresponding `Write*` paths or saved files become unreadable.
- **Public fields in data-carrier classes.** `npcgen.cs` reads/writes them in declaration order;
  do not auto-refactor to properties or reorder.

## License

GNU General Public License v3.0 — see [`LICENSE`](LICENSE).

## TODO

- TODO: document the exact `npcgen.data` binary layout per version (v6 / v>6 / v>9 / v>=11).
- TODO: clarify the expected text encoding for fixed-length name fields in `npcgen.cs`
  (GBK/CP936 vs ASCII/UTF-8 per call site).
- TODO: add a real automated test project (current ad-hoc recipe is manual).
- TODO: confirm minimum supported Windows version and any localization assumptions for the
  `coord_data.txt` decimal separator.
- TODO: add screenshots of the main form to this README.
- TODO: decide whether to drop the now-unused `Microsoft.VisualBasic` reference from the csproj.
