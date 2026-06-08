# PaveMetric

PaveMetric is a Windows Forms desktop application for pavement photo review, perspective correction, and road surface defect assessment.

The application was originally built as `PPR` and stores its working project data in `.ppr` files. The solution is kept under [`source/PPR.sln`](source/PPR.sln).

## Features

- Import pavement photos from a folder of `.jpg` files
- Navigate road sections by photo/chainage
- Calibrate pavement geometry with near/far distance lines and left/right road edges
- Normalize photos with perspective correction
- Configure pavement width, section length, and correction grid density
- Mark, show, hide, and delete defect layers
- Save and reopen `.ppr` project files
- Import FRODO `.log` data
- Export defect summaries to `.txt`
- Render marked defect images into an `Errors` output folder

## Supported Defect Types

The current defect model includes:

- Map crack
- Alligator crack
- Longitudinal crack
- Cross crack
- Pothole
- Filled pothole
- Surface peel-off
- Surface perspiration

## Repository Layout

```text
.
|-- README.md
|-- .gitignore
`-- source
    |-- PPR.sln
    `-- PPR
        |-- PPR.csproj
        |-- MainForm.cs
        |-- DrawingArea.cs
        |-- Project.cs
        |-- PerspectiveCorrection.cs
        |-- Graphics/
        `-- Properties/
```

## Requirements

- Windows
- Visual Studio 2022 or newer with the .NET desktop development workload
- .NET 8 SDK

## Build

Open the solution in Visual Studio:

```text
source/PPR.sln
```

Then build the `PPR` project with the desired configuration.

From PowerShell, the complete solution can be built with the .NET CLI:

```powershell
dotnet build source\PPR.sln -c Release
```

Run the geometry regression checks with:

```powershell
dotnet run --project source\PPR.GeometryTests\PPR.GeometryTests.csproj -c Release
```

The built executable is generated under:

```text
source/PPR/bin/Release/net8.0-windows/
```

## Basic Workflow

1. Start the application.
2. Import a folder containing `.jpg` pavement photos.
3. Select a section/photo from the section selector.
4. Set the section length and pavement width.
5. Mark the far distance, near distance, left edge, and right edge reference lines.
6. Run normalization to apply perspective correction.
7. Add defect markings using the configured defect layers.
8. Save the project as a `.ppr` file.
9. Export defect reports or rendered defect images as needed.

## Defect Editing

- Activate a defect layer, then drag on the pavement image to create a defect rectangle.
- Press `Escape` to return to selection mode.
- Click a visible defect to select it.
- Drag a corner handle to resize the selected defect.
- Press `Delete` to remove the selected defect.
- Press `Ctrl+Z` to undo.
- Press `Ctrl+Y` or `Ctrl+Shift+Z` to redo.
- After perspective normalization, use the `Felülnézet` toolbar button to switch between the original perspective image and a rectified top-down view.

## Input Conventions

Photo files are expected to be `.jpg` images. During import, the application uses the file name without extension as the photo identifier and attempts to derive the section value from it.

For example:

```text
0+120.jpg
```

is interpreted as a chainage-like section value after removing the `+` character.

## Generated Files

The repository intentionally excludes local Visual Studio state and build output:

- `.vs/`
- `bin/`
- `obj/`
- `*.pdb`
- cache and user-specific IDE files

These files are generated locally and should not be committed.

## Project Status

The application has been migrated from .NET Framework 4.8 to an SDK-style .NET 8 Windows Forms project. The original desktop workflow and `.ppr` project format remain intact.
