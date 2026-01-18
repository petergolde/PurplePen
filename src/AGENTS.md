# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is PurplePen?

PurplePen is a desktop course setting program for orienteering races. It allows users to design orienteering courses on maps, manage control points, create course descriptions, and export materials for printing and race management.

## Build and Test Commands

### Building
```bash
# Build the entire solution (Visual Studio 2022 required)
msbuild PPen.sln /p:Configuration=Release

# Build just the main application
msbuild PurplePen/PurplePen.csproj /p:Configuration=Release
```

### Testing
```bash
# Run all tests (uses MSTest framework)
dotnet test PPen.sln

# Run tests without UI popups (headless mode)
dotnet test PPen.sln --settings:.runsettings

# Run tests with UI popups (for debugging)
dotnet test PPen.sln --settings:ShowUi.runsettings

# Run a specific test project
dotnet test PurplePen_Tests/PurplePen_Tests.csproj

# Run a specific test class or method
dotnet test --filter "FullyQualifiedName~CreateBitmapTests"
```

**Important**: Tests use environment variable `TEST_SILENTRUN` to control UI behavior:
- `True` (default in .runsettings): Tests fail if UI would appear
- `False` (ShowUi.runsettings): Tests show actual UI for debugging

### MapModel Subsystem Tests
```bash
# Graphics2D tests
dotnet test MapModel/Graphics2D.Tests/Graphics2D.Tests.csproj

# Rendering backend tests
dotnet test MapModel/Map_PDF.Tests/Map_PDF.Tests.csproj
dotnet test MapModel/Map_Skia.Tests/Map_Skia.Tests.csproj
```

## High-Level Architecture

### Core Design: MVC with Strategy Pattern for Rendering

PurplePen uses a layered architecture with clear separation of concerns:

```
PurplePen (UI + Business Logic)
    ├── Controller: Command pattern coordinator (all operations flow through here)
    ├── EventDB: Course and event data with undo/redo support
    ├── SelectionMgr: Current selection state and active course view
    ├── MapDisplay: Combined map + course rendering
    └── CourseView/CourseLayout: Static course snapshots for rendering
         │
         ├── MapModel: Map data (OCAD/OpenMapper files, symbols, colors)
         └── Graphics2D: Rendering abstraction (IGraphicsTarget interface)
              │
              └── Multiple rendering backends (GDI+, Skia, WPF, PDF, Direct2D, iOS)
```

### Key Architectural Patterns

#### 1. Multi-Backend Rendering (Strategy Pattern)
The most important architectural feature is the **rendering abstraction through `IGraphicsTarget`**.

**Location**: `MapModel/Graphics2D-Shared/IGraphicsTarget.cs`

**Purpose**: Enables the same map and course data to render to multiple outputs:
- **Map_GDIPlus**: Windows GDI+ (screen display, printing) - most mature
- **Map_SkiaStd**: SkiaSharp (cross-platform) - actively being developed
- **Map_WPF**: WPF rendering and XPS printing
- **Map_PDF**: PDF export using PdfSharp
- **Map_D2D**: Direct2D (hardware accelerated)
- **Map_iOS**: iOS/mobile support

Each backend implements `IGraphicsTarget` to translate abstract drawing commands (lines, paths, fills, text) to platform-specific APIs.

#### 2. Shared Code Projects
Uses `.projitems` shared projects to enable code reuse:
- `MapModel/Graphics2D-Shared/`: Shared rendering utilities (Geometry, Bezier, CmykColor, RectSet)
- `MapModel/MapModel-Shared/`: Shared map model code

This allows the same code to compile for .NET Framework, .NET Standard, and iOS platforms.

#### 3. CMYK Color Model
**All colors use CMYK** for print accuracy (critical for orienteering maps).

**Location**: `MapModel/Graphics2D-Shared/CmykColor.cs`

Each rendering backend converts CMYK → RGB/platform color as needed. Never assume RGB colors in map or symbol code.

#### 4. Controller Command Pattern
**Location**: `PurplePen/Controller.cs` (very large file ~52K tokens)

**All UI operations flow through the Controller**, which coordinates:
- EventDB changes (with undo/redo)
- SelectionMgr updates
- MapDisplay rendering
- File I/O operations
- Printing and export

When modifying functionality, look for command methods in Controller.cs first.

#### 5. EventDB with Undo/Redo
**Location**: `PurplePen/EventDB.cs`

EventDB wraps all course data (courses, controls, legs, special objects) with automatic undo/redo support through `UndoMgr`. All data modifications must go through EventDB methods to maintain undo history.

#### 6. Immutable Course Views
**Location**: `PurplePen/CourseView.cs`

CourseView creates static snapshots of courses for rendering. This separates the mutable data model (EventDB) from the immutable rendering view, enabling complex course variations (relay, map exchanges) without affecting the source data.

## Project Structure

### Main Application
**PurplePen/** - WinForms desktop application
- Controller.cs: Central command coordinator
- EventDB.cs: Course data model with undo
- SelectionMgr.cs: Selection state management
- MapDisplay.cs: Combined map + course display
- CourseView.cs: Immutable course snapshots
- CourseLayout.cs: Converts courses to drawable objects
- SymbolDB.cs: Orienteering symbols database

### MapModel Subsystem
**MapModel/MapModel/** - Core map data model
- Map.cs: Orienteering map with symbols and templates
- SymDef.cs: Symbol definitions (point, line, area, text)
- Symbol.cs: Symbol instances on the map
- SymColor.cs: CMYK color definitions
- OcadImport/Export: OCAD file format I/O (versions 6-12)
- OpenMapperImport/Export: OpenMapper format I/O

**MapModel/Graphics2D-Shared/** - Rendering abstraction
- IGraphicsTarget.cs: Core rendering interface
- Geometry.cs, Bezier.cs: Geometric utilities
- CmykColor.cs: CMYK color support
- RectSet.cs: Rectangle set operations

**MapModel/Map_[Backend]/** - Rendering implementations
- Each implements IGraphicsTarget for its platform
- GraphicsTarget.cs: Main implementation file

## File Format Support

PurplePen supports multiple map file formats:
- **OCAD**: Industry standard (versions 6, 7, 8, 9, 10, 11, 12)
- **OpenMapper**: Open-source alternative (.omap files)
- **PDF templates**: For georeferenced backgrounds
- **Bitmap templates**: With georeferencing support

Course files are stored in Purple Pen's XML format (.ppen files).

## Important Conventions

### When Adding/Modifying Rendering Code
1. **Never implement rendering logic directly in UI code** - use IGraphicsTarget abstraction
2. **Add new rendering features to IGraphicsTarget interface** - then implement in all backends
3. **Test against multiple backends** - especially GDI+ (production) and Skia (future)
4. **Use CMYK colors** - never assume RGB
5. **Recent commits show Skia work** - that backend is actively being migrated to

### When Modifying Course/Event Data
1. **All changes must go through EventDB** - maintains undo history
2. **Use Controller methods** - don't modify EventDB directly from UI
3. **Update CourseView snapshots** - when course data changes
4. **Consider undo/redo** - ensure operations are reversible

### When Working with Maps
1. **Symbols are defined by SymDef, instantiated as Symbol** - two separate classes
2. **OCAD format is complex** - use existing import/export code, consult OCAD specs
3. **Map coordinates use map units** - not pixels or screen coordinates
4. **Templates can be georeferenced** - transformations are critical

### Testing
1. **Use TestUI.Create()** - creates test controller instance (see PurplePen_Tests/CreateBitmapTests.cs)
2. **Bitmap comparison tests use MAX_PIXEL_DIFF** - allow for minor rendering differences
3. **Set TEST_SILENTRUN appropriately** - True for CI, False for debugging
4. **Test files in TestFiles/** - use existing test courses and maps
5. **Interactive tests in MapModel/InteractiveTestApp** - for visual verification

### Writing Code
1. **Follow existing coding style** - consistent naming, formatting
2. All classes and method should have a header comment describing purpose and parameters
3. Use explicit types instead of "var".

### Code Analysis
The solution uses custom ruleset files:
- `Tools/PurplePenRules.ruleset`: Main application rules
- `MapModel/Analysis.ruleset`: MapModel subsystem rules
- `MapModel/ExternalCode.ruleset`: External/third-party code

## Current Development Focus

Based on recent git commits:
- **Skia rendering implementation** - migrating from GDI+ to SkiaSharp for cross-platform support
- **Font fallback** - investigating font rendering issues
- **Layer blending** - fixing template rendering with map files
- **Glyph handling** - fixing OpenMapper import issues with empty glyphs

When working on rendering code, be aware of ongoing Skia migration work.
