# PaveMetric

**PaveMetric** is a Windows desktop application for the objective assessment of road surface condition using photogrammetric methods. It transforms perspective pavement photographs into rectified top-down views, enabling precise measurement and classification of surface defects.

The software was developed at the **University of Sopron, Institute of Geomatics and Civil Engineering** (*Soproni Egyetem, Geomatikai és Kultúrmérnöki Intézet*).

---

## Background

The methodology implemented in PaveMetric was first published in 2013 in *Útügyi Lapok*, a Hungarian road engineering journal:

> **Markó G. & Primusz P.** (2013): *Útburkolatok felületi állapotának objektív minősítése fotogrammetriai eljárással* [Objective assessment of road surface condition by photogrammetric methods]. *Útügyi Lapok*, 2013. június.  
> https://utugyilapok.hu/2013/06/utburkolatok-feluleti-allapotanak-objektiv-minositese-fotogrammetriai-eljarassal/

The paper describes a field and office workflow for recording surface defects on asphalt-paved roads. Photographs are taken from eye level at marked stations along the road, then processed in software to derive the exact location and total area of each defect type. Field measurements of approximately 10 m sections yield an accuracy sufficient for repair planning and cost estimation.

---

## Key Features

- **Perspective correction** — calibrate each photo using road edge lines and near/far reference markers to map image pixels to real-world pavement coordinates
- **Rectified top-down view** — toggle between the original perspective image and a corrected overhead projection
- **Defect marking** — draw axis-aligned rectangles over surface defects; each defect belongs to a configurable type layer
- **Configurable defect types** — add, remove, rename, and recolour defect layers at runtime via the type editor toolbox
- **Snap grid** — cursor snaps to a sub-grid aligned to the drawn measurement grid for accurate boundary placement
- **Section navigation** — browse a folder of sequential `.jpg` photos organised by chainage
- **Export** — defect summaries as `.txt` reports and rendered defect overlays into an `Errors` output folder
- **Project files** — save and reopen work as `.ppr` (PaveMetric Project) files
- **Undo / Redo** — full edit history with `Ctrl+Z` / `Ctrl+Y`

---

## Supported Defect Types

| Code | English name | Hungarian name |
|-----:|---|---|
| 10 | Map crack | Hálós repedés |
| 11 | Alligator crack | Hálós repedés deformációval |
| 12 | Longitudinal crack | Hosszirányú repedés |
| 13 | Cross crack | Keresztirányú repedés |
| 14 | Pothole | Kátyú |
| 15 | Filled pothole | Kitöltött kátyú |
| 16 | Surface peel-off | Felületi hámlás |
| 17 | Surface perspiration | Izzadás |

The type list is fully configurable in the running application; new types can be added and existing ones deleted or renamed.

---

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) with the **.NET desktop development** workload, or the .NET CLI

---

## Build

Open the solution in Visual Studio:

```
source/PaveMetric.sln
```

Or build from the command line:

```powershell
dotnet build source\PaveMetric.sln -c Release
```

Run the geometry regression tests:

```powershell
dotnet run --project source\PaveMetric.GeometryTests\PaveMetric.GeometryTests.csproj -c Release
```

The built executable is placed under:

```
source/PaveMetric.WinForm/bin/Release/net8.0-windows/
```

---

## Basic Workflow

1. **Import** a folder of `.jpg` pavement photos (`File › Import`).
2. **Select** a section from the section navigator.
3. **Set geometry** — enter the pavement width and section length.
4. **Calibrate** — mark the far distance, near distance, left edge, and right edge reference lines on the image.
5. **Normalize** — run perspective correction; toggle the top-down view with the *Felülnézet* toolbar button.
6. **Mark defects** — select a defect layer, then drag on the image to draw defect rectangles.
7. **Save** the project as a `.ppr` file.
8. **Export** — generate text reports or rendered defect images.

### Defect editing shortcuts

| Action | Input |
|---|---|
| Draw defect rectangle | Drag on image (active layer selected) |
| Cancel drawing | `Escape` |
| Select defect | Click on a visible defect |
| Resize defect | Drag a corner handle |
| Delete selected defect | `Delete` |
| Undo | `Ctrl+Z` |
| Redo | `Ctrl+Y` or `Ctrl+Shift+Z` |

---

## Photo Naming Convention

Photos should be named after their starting chainage, for example:

```
0+120.jpg   →  section starting at chainage 0+120
0+130.jpg   →  next section
```

The `+` character is stripped when parsing the section value.

---

## Repository Layout

```
.
├── README.md
└── source
    ├── PaveMetric.sln
    ├── PaveMetric.WinForm/          # Windows Forms application
    │   ├── PaveMetric.WinForm.csproj
    │   ├── MainForm.cs
    │   ├── DrawingArea.cs
    │   ├── PerspectiveCorrection.cs
    │   ├── Project.cs
    │   ├── ErrorLayerControl.cs
    │   ├── ErrorTypeToolbox.cs
    │   ├── Theme.cs
    │   └── Graphics/
    └── PaveMetric.GeometryTests/    # Geometry regression tests
        └── PaveMetric.GeometryTests.csproj
```

---

## Authors

| Role | Name | Affiliation |
|---|---|---|
| Original developer | **Markó Gergely** | Budapest University of Technology and Economics, Department of Highway and Railway Engineering |
| Current developer | **Primusz Péter** | University of Sopron, Institute of Geomatics and Civil Engineering |

---

## Institution

**Soproni Egyetem — Geomatikai és Kultúrmérnöki Intézet**  
University of Sopron — Institute of Geomatics and Civil Engineering  
https://uni-sopron.hu
