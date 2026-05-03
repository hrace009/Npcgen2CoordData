# Npcgen2CoordData

A Windows Forms utility tool for game server administrators to import NPC/mob spawn positions and resource spawn positions from `npcgen.data` binary files into `coord_data.txt`.

## Overview

`Npcgen2CoordData` reads the binary `npcgen.data` file (used in Perfect World-based game servers) and extracts the 3D coordinates of mob spawns and resource spawns, then writes them into the `coord_data.txt` format used by the game server.

## Features

- Load and parse `npcgen.data` binary files (supports file versions up to v11+)
- Load existing `coord_data.txt` files
- Import mob spawn coordinates from `npcgen.data` into `coord_data.txt`
- Import resource spawn coordinates from `npcgen.data` into `coord_data.txt`
- Specify a custom map/location name (e.g., `world`, `a78`, `a64`) during import
- Save the updated `coord_data.txt` file
- Progress bar and status indicator during file operations

## How It Works

### Input: `npcgen.data`

A binary file containing:
- **NPC/Mob Spawns** — spawn groups with 3D positions, directions, and a list of mob entries (ID, respawn time, aggression, path, etc.)
- **Resource Spawns** — resource groups with 3D positions and a list of resource entries (ID, type, respawn time, amount)
- **Dynamic Objects** — static world objects with positions and rotation
- **Triggers** — scheduled event triggers (present in file version > 6)

### Output: `coord_data.txt`

A tab-separated text file with the following column structure:

```
<NPC/Resource ID>   <MapName>   <X>   <Y>   <Z>
```

Each entry maps an NPC or resource ID to its world coordinates on a specific map.

## Usage

1. **Load coord_data.txt** — Click **Load Coord Data** and select your existing `coord_data.txt` file.
2. **Import npcgen.data** — Click **Import** and select the `npcgen.data` file. Enter the map location name when prompted (e.g., `world`, `a78`).
3. **Save coord_data.txt** — Click **Save Coord Data** and choose a destination path to save the updated file.

## Requirements

- Windows OS
- .NET Framework 3.5

## Project Structure

| File | Description |
|------|-------------|
| `Form1.cs` | Main UI form — handles file dialogs, threading, and import logic |
| `npcgen.cs` | Binary parser and writer for `npcgen.data`; defines all data model classes |
| `CoordData.cs` | Reader and writer for `coord_data.txt` |
| `Program.cs` | Application entry point |

## Build

Open `Npcgen2CoordData.sln` in Visual Studio and build the solution. Targets **.NET Framework 3.5**, compatible with Visual Studio 2017 and later.
