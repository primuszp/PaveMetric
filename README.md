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
- Visual Studio 2019 or newer
- .NET Framework 4.8 Developer Pack
- MSBuild for .NET Framework projects

## Build

Open the solution in Visual Studio:

```text
source/PPR.sln
```

Then build the `PPR` project with the desired configuration.

From a Visual Studio Developer PowerShell, the project can also be built with MSBuild:

```powershell
msbuild source\PPR.sln /p:Configuration=Release /p:Platform="Any CPU"
```

If `msbuild` is not available on `PATH`, use the full Visual Studio MSBuild path, for example:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" source\PPR.sln /p:Configuration=Release /p:Platform="Any CPU"
```

The built executable is generated under:

```text
source/PPR/bin/Release/
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

This is a legacy .NET Framework Windows Forms codebase. The current cleanup keeps the original application structure intact while making the repository suitable for GitHub hosting and future maintenance.
