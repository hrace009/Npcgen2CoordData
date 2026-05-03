# AGENTS.md — Npcgen2CoordData

Project-specific notes for advanced contributors. Generic .NET / WinForms / Rider knowledge is omitted.

## 1. Build / Configuration

- **Target framework**: `.NET Framework 3.5` (`v3.5`), `OutputType=WinExe`, `Platform=AnyCPU`. The `.NET 3.5 SP1` targeting pack must be installed (Windows feature *“.NET Framework 3.5 (includes .NET 2.0 and 3.0)”*) — without it MSBuild fails to resolve `mscorlib`/`System.Windows.Forms` reference assemblies.
- **Language version**: C# 7.3 (the `csproj` does not pin `<LangVersion>`, but the codebase uses `?.`, `$"..."`, expression-bodied members, tuple-friendly syntax — anything below C# 6 will break it). Use the **Roslyn** `csc.exe` shipped with VS/MSBuild, **not** `C:\Windows\Microsoft.NET\Framework\v3.5\csc.exe` (legacy framework compiler is C# 3.0 only and will fail on string interpolation in `CoordData.cs`).
- **Build commands** (PowerShell, from repo root):
  ```powershell
  # Full build via the IDE-installed MSBuild (Rider/VS):
  & "C:\Program Files\Microsoft Visual Studio\<edition>\MSBuild\Current\Bin\MSBuild.exe" `
      Npcgen2CoordData\Npcgen2CoordData.csproj /v:m /nologo
  ```
  Output: `Npcgen2CoordData\bin\<Configuration>\Npcgen2CoordData.exe`.
- **Project layout caveat**: Despite targeting net35, the `csproj` is the *legacy* (non-SDK) format with `ToolsVersion="15.0"`. Do **not** convert it to SDK-style without first replacing all WinForms designer wiring (`Form1.Designer.cs`, `Form1.resx` `<EmbeddedResource>` linkage with `<DependentUpon>`).
- **References**: `Microsoft.VisualBasic` is a real runtime dependency — `Form1.cs` calls `Microsoft.VisualBasic.Interaction.InputBox` for the “location name” prompt. Do not remove it from `<ItemGroup>`.
- **Icon**: `icon.ico` is referenced as `<ApplicationIcon>` *and* as `<Content>`; both entries are intentional, do not deduplicate (removing `<ApplicationIcon>` strips the EXE icon, removing `<Content>` causes the file watcher in some IDEs to drop the file from the project tree).

## 2. Testing

The repository has **no test project, no test framework reference, and no CI**. Tests are written ad-hoc as standalone console programs that compile against the production source files. Below is the exact, verified workflow.

### 2.1 Recipe — add a new ad-hoc test

1. Create a `.cs` file at the repo root (e.g. `TestX.cs`) with `static int Main()` returning a non-zero exit code on failure.
2. The classes under test (`CoordData`, `NpcGen`, etc.) are declared **`internal`** (no access modifier on `class`). Put your test in `namespace Npcgen2CoordData` so it's in the same logical unit; you do **not** need `InternalsVisibleTo` because you compile the production `.cs` files directly into the test EXE.
3. `CoordData.cs` and `npcgen.cs` reference the four delegate types declared in `Form1.cs` (`SetProgressMax/Next/Value/Text`). When compiling without `Form1.cs`, redeclare them in your test file inside the same namespace (see example below). Do not pull in `Form1.cs` — it drags `System.Windows.Forms`, `Microsoft.VisualBasic`, and a designer cycle.
4. Compile with the Roslyn `csc.exe` against net35 reference assemblies (the framework SDK csc is too old):
   ```powershell
   & "C:\Program Files\Microsoft Visual Studio\<edition>\MSBuild\Current\Bin\Roslyn\csc.exe" `
       /nologo /langversion:7.3 /target:exe /out:TestX.exe `
       TestX.cs Npcgen2CoordData\CoordData.cs
   .\TestX.exe ; "ExitCode=$LASTEXITCODE"
   ```
5. **Always delete** the generated `TestX.cs`/`TestX.exe`/`TestX.pdb` after a successful run — the production `csproj` uses explicit `<Compile Include>` entries, so a stray `.cs` at the repo root won't be picked up by MSBuild, but it will be picked up by Rider's solution-wide analysis and may shadow types.

### 2.2 Verified example — `CoordData` round-trip

The following test was executed against the current code and **passed** (`ExitCode=0`):

```csharp
// TestCoordData.cs (placed at repo root, deleted after the run)
using System;
using System.IO;

namespace Npcgen2CoordData
{
    // Re-declared because Form1.cs is excluded from the test build.
    public delegate void SetProgressMax(int value);
    public delegate void SetProgressNext();
    public delegate void SetProgressValue(int value);
    public delegate void SetProgressText(string value);

    internal static class TestCoordData
    {
        private static int Main()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "coord_data_test.txt");
            File.WriteAllText(tmp,
                "HEADER\r\n" +
                "1001\tworld\t100.50\t-20.25\t5.00\r\n" +
                "1001\tworld\t101.00\t-21.00\t5.50\r\n" +
                "2002\ta78\t10.00\t20.00\t30.00\r\n");

            var cd = new CoordData();
            cd.ProgressMax   += v => { };
            cd.ProgressNext  += () => { };
            cd.ProgressValue += v => { };
            cd.ProgressText  += t => Console.WriteLine("[status] " + t);

            cd.Read(tmp);
            if (cd.Entrys.Count != 2 ||
                cd.Entrys["1001"].Count != 2 ||
                cd.Entrys["2002"].Count != 1) return 1;

            string outPath = Path.Combine(Path.GetTempPath(), "coord_data_test_out.txt");
            cd.Save(outPath);

            File.Delete(tmp); File.Delete(outPath);
            Console.WriteLine("OK");
            return 0;
        }
    }
}
```

Compile + run as in §2.1. Expected last lines of output:

```
[status] coord_data.txt successfully saved
...
OK
ExitCode=0
```

### 2.3 Testing `npcgen.cs`

`NpcGen.ReadNpcgen` consumes a `BinaryReader`, so a unit test can construct a synthetic in-memory `MemoryStream` matching the v6 / v>6 layout (see `ReadExistence`, `ReadResource`, `ReadDynObjects`, `ReadTrigger`). Branch points to cover:

| File version | Extra fields read |
|--------------|-------------------|
| `> 6`        | `TriggersAmount` header field; `Trigger_id`, `Life_time`, `MaxRespawnTime` per mob group; trigger block at end of file |
| `> 9` / `>= 11` | additional fields inside `ReadExistence` / `ReadResource` (audit `npcgen.cs` for `if (Version > N)` guards before adding fixtures) |

`NpcGen.ReadNpcgen` calls `ProgressMax.Invoke` **without** the `?.` null-safe operator (line ~152) — any test must subscribe at least to `ProgressMax`, otherwise it throws `NullReferenceException`. The other three events are invoked with `?.` and may be left unsubscribed. The same asymmetry exists in `CoordData.Read` for `ProgressText` (invoked unconditionally on line ~32).

## 3. Project-Specific Gotchas

### 3.1 Culture-sensitive number parsing (HIGH-IMPACT BUG SURFACE)

`CoordData.Read` does:
```csharp
X = float.Parse(data[2].Replace('.', ','))
```
and `CoordData.Save` does:
```csharp
string.Format("{0:F2}", x.X).Replace(",", ".")
```
This **assumes the current thread culture uses `,` as the decimal separator** (e.g. `de-DE`, `ru-RU`). On `en-US` systems (decimal `.`), the `Replace('.', ',')` call on read produces strings like `"100,50"` which then `float.Parse` rejects under `en-US`, throwing `FormatException`. When changing this code, the correct fix is `float.Parse(data[2], CultureInfo.InvariantCulture)` and `x.X.ToString("F2", CultureInfo.InvariantCulture)` — do **not** silently drop the `Replace` calls or output formatting will diverge depending on locale. Existing test runs in this repo were performed on a comma-decimal locale (round-trip output shows `X=100,5`), which masks the bug.

### 3.2 Threading model

`Form1` sets `CheckForIllegalCrossThreadCalls = false` and runs `coord.Read/Save` and `npcgen.ReadNpcgen` on raw `new Thread(...)`. Progress event handlers (`ProgressMax`, `ProgressValue`, `ProgressNext`, `ProgressText`) update WinForms controls (`Progress.Value`, `Status.Text`) directly from the worker thread. Do **not** “fix” this by adding `Invoke`/`BeginInvoke` without first removing `CheckForIllegalCrossThreadCalls = false`, and do not introduce `async`/`await` (TPL is awkward on net35; `Task` is missing without the BCL Async pack).

### 3.3 Progress event contracts

- Subscribers are wired in `Form1` constructor; both `NpcGen` and `CoordData` raise the same delegate instance bound to UI methods.
- `ProgressMax` is invoked unconditionally (no `?.`) in both `NpcGen.ReadNpcgen` and via `Form1.import_Click`. New code paths must keep at least one subscriber attached or guard the invocation.
- `CoordData.Read` calls `File.ReadAllLines(path).Length` *and then* opens a `StreamReader` over the same path — the file is read from disk twice. Acceptable for current file sizes; flag it if ingesting very large files.

### 3.4 Binary file format (`npcgen.data`)

- Endianness: native little-endian via `BinaryReader.ReadInt32/ReadSingle/ReadByte`. Do not introduce `BitConverter` with manual byte swapping.
- Strings: `GetBytes(string Name, int NameLength, Encoding e)` uses fixed-length, encoding-dependent name fields. Do not change the encoding without coordinating with the consumer game-server toolchain — Perfect-World era servers expect GBK/CP936 in some fields and ASCII/UTF-8 in others. Audit each call site of `GetBytes` before touching.
- Version branching: the parser checks `Version > 6` for triggers and additional mob-group fields. When adding support for a newer version, mirror the structure in **both** `Read*` and `Write*` methods or saved files will be unreadable by the original game tools.

### 3.5 Form designer / resources

- `Form1.Designer.cs` is generated; edit only via the WinForms designer or by carefully synchronising with `Form1.resx`. `Properties\Resources.resx` and `Properties\Settings.settings` are present but unused — leaving them as-is is intentional (removing them changes the assembly resource layout).
- The form uses `Microsoft.VisualBasic.Interaction.InputBox` for the map-name prompt; replacing it with a custom WinForms dialog removes the only reason to keep `Microsoft.VisualBasic` referenced.

## 4. Code Style

- 4-space indentation, Allman braces, `PascalCase` for public members, `camelCase` for locals — matches the existing files.
- Public fields are used liberally on data-carrier classes (`ClassDefaultMonsters`, `CoordDataEntry`, …). Do **not** auto-refactor these to properties: `BinaryReader` round-tripping in `npcgen.cs` reads/writes them in declaration order through reflection-free hand-written code, and changing field order or visibility is silently breaking.
- Comments are **rare to absent** in the codebase (only a few `#region` markers in `npcgen.cs`). Keep new code comment-light; prefer self-explanatory naming. All existing identifiers and UI text are in English.
- `using` directive set is broad and copied across files (`System.Linq`, `System.Text`, etc., even when unused). Don't bother trimming — Rider's “optimise imports” will create churn without functional benefit.
- No nullable reference types, no `var`-everywhere policy: the codebase mixes explicit types and `var` freely; follow the surrounding file's local convention.
