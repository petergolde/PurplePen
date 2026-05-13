# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is PurplePen?

PurplePen is a desktop course setting program for orienteering races. It allows users to design orienteering courses on maps, manage control points, create course descriptions, and export materials for printing and race management.

## Code and Solution file

All of the code is in the "src" subdirectory. Ignore the "doc" and "loc" directories,
which contain documentation and localization files.

The PPen.slnx solution file is the solution file for building the application.

## Build and Test Commands

### Building
```bash
# Build the entire solution (Visual Studio 2022 required)
msbuild PPen.slnx /p:Configuration=Release

# Build just the main application
msbuild PurplePen/PurplePen.csproj /p:Configuration=Release
```

### Testing
```bash
# Run all tests (uses MSTest framework)
dotnet test PPen.slnx

# Run tests without UI popups (headless mode)
dotnet test PPen.slnx --settings:.runsettings

# Run tests with UI popups (for debugging)
dotnet test PPen.slnx --settings:ShowUi.runsettings

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

### Avalonia Cross-Platform Application (active development)

PurplePen is being ported from WinForms (PurplePen/) to Avalonia (AvPurplePen/) for cross-platform support. The Avalonia app follows MVVM with CommunityToolkit.Mvvm source generators.

**AvPurplePen/** - Avalonia desktop application (Views and platform-specific code)
- Namespace: `AvPurplePen`
- Views/: AXAML views with code-behind (e.g., MainWindow.axaml)
- ViewLocator.cs: Convention-based IDataTemplate that maps ViewModels to Views automatically. Maps `PurplePen.ViewModels.FooViewModel` → `AvPurplePen.Views.FooView`. Used when a ViewModel appears as Content of a ContentControl; not used for MainWindow (created directly in App.axaml.cs).
- UIText.resx: Localized UI strings (English). Translated variants: UIText.fr.resx, UIText.de.resx, etc.
- LocalizeExtension.cs: Custom XAML markup extension for localization with live language switching (see Localization section below).
- App.axaml.cs: Application startup, creates MainWindow, registers ViewLocator as a DataTemplate.
- Program.cs: Entry point. DI container is set up before the Avalonia builder line (safe because DI is plain .NET, not Avalonia-dependent).

**PurplePenViewModels/** - ViewModels (separate project, no UI dependencies)
- Namespace: `PurplePen.ViewModels`
- Uses CommunityToolkit.Mvvm source generators: `[ObservableProperty]` for properties, `[RelayCommand]` for commands. Classes must be `partial`.
- ViewModelBase.cs: Abstract base class inheriting `ObservableObject`.
- ViewModels do NOT contain localized strings or UI text — that belongs in the View layer (UIText.resx).
- ViewModels must have a parameterless constructor (or a parameterless constructor in addition to others) so the Avalonia designer can instantiate them in `<Design.DataContext>`.
- References PurplePenCore but NOT AvPurplePen (ViewModels must not depend on Views).
- **Only allowed dependencies**: PurplePenCore (project) and CommunityToolkit.Mvvm (package). Do NOT add Avalonia or any other package/project references. Platform-specific types (e.g., Avalonia.Platform.Storage) must not be used in ViewModels.

**PurplePenViewModels.Tests/** - NUnit tests for ViewModels
- Uses NUnit framework (`[TestFixture]`, `[Test]`, `[SetUp]`)
- Tests command execution via `ICommand.Execute(null)` and verifies PropertyChanged notifications.

#### Key MVVM Conventions
- **Compiled bindings**: All AXAML files use `x:DataType` for compile-time checked bindings.
- **Namespace mapping in XAML**: `xmlns:vm="using:PurplePen.ViewModels"` for ViewModels, `xmlns:resx="using:AvPurplePen"` for resource/localization classes.
- **DataContext is set by the caller**, not by the View itself. The View's code-behind constructor only calls `InitializeComponent()`. The parent that opens the dialog creates the ViewModel and assigns it to `DataContext` before showing.
- **Design-time DataContext**: Every View should have `<Design.DataContext><vm:FooViewModel/></Design.DataContext>` so the designer previewer can show bindings. This requires ViewModels to have a parameterless constructor.

#### Localization System

The localization system supports **live language switching** — all controls update instantly when the user changes languages, without restarting the application.

**Key classes** (all in `AvPurplePen/LocalizeExtension.cs`):
- `LocalizeExtension`: XAML markup extension. At design time, returns the English string from `UIText.ResourceManager` so the VS previewer shows real text. At runtime, returns a `Binding` to a `LocalizedString.Value` property that updates on language change.
- `LocalizedString`: Wraps a single resource key. Its `Value` property reads from `ResourceManager` using `CultureInfo.CurrentUICulture`. Raises `PropertyChanged` when `Refresh()` is called.
- `LocalizedStringManager`: Singleton that manages `LocalizedString` instances (one per unique key, stored in a dictionary). Call `NotifyLanguageChanged()` after changing `CurrentUICulture` to refresh all bindings.

**XAML usage for localized strings**:
```xml
<!-- Simple text — use {resx:Localize} for live language switching -->
<Button Content="{resx:Localize AboutForm_okButton_Text}"/>
<TextBlock Text="{resx:Localize AboutForm_copyrightLabel_Text}"/>
<Window Title="{resx:Localize AboutForm_Text}"/>

<!-- Formatted strings (e.g. "Version {0}") — use {x:Static} for StringFormat -->
<!-- NOTE: {resx:Localize} CANNOT be used in StringFormat because it returns a Binding, not a string -->
<TextBlock.Text>
    <Binding Path="PrettyVersion"
             StringFormat="{x:Static resx:UIText.MiscText_VersionLabel}" />
</TextBlock.Text>
```

**Resource key naming convention**:
- Form/dialog properties: `FormName_Text` (e.g. `AboutForm_Text` for the window title)
- Control text: `FormName_controlName_Text` (e.g. `AboutForm_okButton_Text`)
- Shared strings: descriptive name without form prefix (e.g. `OKButton`, `CancelButton`)

**Changing language at runtime**:
```csharp
CultureInfo newCulture = new CultureInfo(languageCode);
Thread.CurrentThread.CurrentUICulture = newCulture;
CultureInfo.DefaultThreadCurrentUICulture = newCulture;
LocalizedStringManager.Instance.NotifyLanguageChanged();
```

### Legacy WinForms Application
**PurplePen/** - WinForms desktop application (being ported to AvPurplePen)
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
- Each backend also has `IGraphicsBitmap` implementations (e.g., `Skia_Bitmap`, `Skia_Image`, `Skia_Pixmap`, `GDIPlus_Bitmap`)

**MapModel/Map_SkiaStd/BitmapIO.cs** - Bitmap I/O using ImageSharp
- Uses SixLabors.ImageSharp for reading/writing bitmap metadata (DPI resolution, format detection)
- Uses SkiaSharp for pixel decoding (falls back to ImageSharp for formats Skia can't decode, e.g. TIFF)
- Key types: `BitmapWithResolution`, `PixmapWithResolution` — hold an SKBitmap/SKPixmap plus format and DPI
- The `SkiaBitmapGraphicsLoader` class (in SkiaGraphicsTarget.cs) uses `BitmapIO` and returns `Skia_Bitmap` instances with resolution

### IGraphicsBitmap Implementations and Resolution
The `IGraphicsBitmap` interface (in `Graphics2D/IGraphicsTarget.cs`) defines `HorizontalResolution` and `VerticalResolution` properties (DPI, default 96).

**Skia backend** (`MapModel/Map_SkiaStd/SkiaGraphicsTarget.cs`):
- `Skia_Bitmap`: Wraps `SKBitmap`. Stores resolution in fields. Has constructors with and without resolution. `Crop()` returns a `Skia_Pixmap` preserving resolution.
- `Skia_Image`: Wraps `SKImage`. Stores resolution in fields. `Crop()` returns `Skia_Pixmap` or `Skia_Image` preserving resolution. `WriteToStream()` uses stored resolution.
- `Skia_Pixmap`: Wraps `SKPixmap`. Stores resolution in fields. `Crop()` preserves resolution. `WriteToStream()` uses stored resolution.

**GDI+ backend** (`MapModel/Map_GDIPlus/GraphicsTarget.cs`):
- `GDIPlus_Bitmap`: Delegates resolution to `System.Drawing.Bitmap.HorizontalResolution`/`VerticalResolution`. `Bitmap.Clone()` in `Crop()` preserves resolution automatically.

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
5. **Bitmap test files in MapModel/TestFiles/bitmaps/** - includes resolution test images (e.g., `Waterfall.jpg`/`.png` at 230 DPI)
6. **Interactive tests in MapModel/InteractiveTestApp** - for visual verification
7. **ViewModel tests in PurplePenViewModels.Tests/** - NUnit tests for ViewModels (no UI dependencies). Test commands via `ICommand.Execute(null)` and verify `PropertyChanged` notifications.
8. **MapModel tests use NUnit** (`[TestFixture]`, `[Test]`). PurplePen_Tests uses MSTest.

### Writing Code
1. **Follow existing coding style** - consistent naming, formatting
2. All classes and method should have a header comment describing purpose and parameters
3. Use explicit types instead of "var".

### Code Analysis
The solution uses custom ruleset files:
- `Tools/PurplePenRules.ruleset`: Main application rules
- `MapModel/Analysis.ruleset`: MapModel subsystem rules
- `MapModel/ExternalCode.ruleset`: External/third-party code

## Porting WinForms Dialogs to Avalonia

PurplePen forms are being migrated one-by-one from `PurplePen/` (WinForms) to `AvPurplePen/Views/` (Avalonia AXAML) + `PurplePenViewModels/` (ViewModels). The goal is to produce Avalonia AXAML with correctly named controls, localized strings, and the same visual layout — then wire up data binding and logic afterward.

### Source Files to Read

Each WinForms form consists of two key files:
- **`FormName.Designer.cs`**: Control types, names, parent-child hierarchy, event handlers. Uses `resources.ApplyResources(control, "name")` — actual property values are NOT here.
- **`FormName.resx`**: The actual property values (Location, Size, Text, Anchor, Dock) and critically, `TableLayoutSettings` XML that defines Grid rows/columns/placement.
- **`FormName.cs`**: Business logic, event handlers, constructor code.

### Control Mapping (WinForms → Avalonia)

| WinForms | Avalonia | Notes |
|---|---|---|
| `Label` | `TextBlock` | |
| `Button` | `Button` | `Text` → `Content`; add `HorizontalContentAlignment="Center"` for fixed-width buttons |
| `TextBox` | `TextBox` | |
| `CheckBox` | `CheckBox` | |
| `RadioButton` | `RadioButton` | |
| `ComboBox` | `ComboBox` | Items need different handling |
| `NumericUpDown` | `NumericUpDown` | |
| `GroupBox` | `HeaderedContentControl` | Or a styled `Border` with header |
| `TableLayoutPanel` | `Grid` | Column/Row definitions from LayoutSettings (see below) |
| `FlowLayoutPanel` | `WrapPanel` or `StackPanel` | Depends on flow direction |
| `Panel` | `Panel` or `Border` | |
| `PictureBox` | `Image` | |
| `TreeView` | `TreeView` | |
| `ListBox` | `ListBox` | |
| `DataGridView` | `DataGrid` | Most complex mapping |
| `LinkLabel` | `HyperlinkButton` or styled `Button` | |
| `ToolStrip/MenuStrip` | `Menu` | Different structure |
| `DockStyle.Top/Bottom` | `DockPanel.Dock="Top/Bottom"` | Use `DockPanel` as parent container |

### TableLayoutPanel → Grid Conversion

The `.resx` files store TableLayoutPanel configuration as embedded XML:
```xml
<TableLayoutSettings>
  <Controls>
    <Control Name="okButton" Row="0" Column="3" RowSpan="1" ColumnSpan="1" />
  </Controls>
  <Columns Styles="AutoSize,0,Percent,100,AutoSize,0" />
  <Rows Styles="AutoSize,0" />
</TableLayoutSettings>
```

Map column/row styles as follows:
- `AutoSize,0` → `Auto`
- `Percent,100` → `*`
- `Percent,50` → `0.5*`
- `Absolute,150` → `150`

Resulting Avalonia Grid:
```xml
<Grid ColumnDefinitions="Auto,*,Auto">
    <Button x:Name="okButton" Grid.Row="0" Grid.Column="2" />
</Grid>
```

### Hiding Controls Without Layout Shift (Opacity Pattern)

In Avalonia, `IsVisible="False"` collapses the element entirely — a Grid row with `Auto` height shrinks to zero when all its children are invisible. This causes the dialog to visually jump when toggling visibility.

**Use Opacity instead of IsVisible** when you want to hide controls but keep their layout space:

1. Add computed `double` opacity properties in the ViewModel (compiled bindings require the correct type — `bool` cannot bind to `Opacity`):
   ```csharp
   public double NormalCourseOpacity => IsNormalCourse ? 1.0 : 0.0;
   public double ScoreCourseOpacity => IsScoreCourse ? 1.0 : 0.0;
   ```
   Remember to add `[NotifyPropertyChangedFor(nameof(NormalCourseOpacity))]` on the source field.

2. In AXAML, use `Opacity` + `IsHitTestVisible` (to block mouse interaction):
   ```xml
   <TextBlock Opacity="{Binding NormalCourseOpacity}" .../>
   <StackPanel Opacity="{Binding NormalCourseOpacity}"
               IsHitTestVisible="{Binding IsNormalCourse}" ...>
       <TextBox Text="{Binding MyField, Mode=TwoWay}" .../>
   </StackPanel>
   ```

**When to use which:**
- `IsVisible` — for ComboBox dropdown items or elements where collapsing is desired
- `Opacity` + `IsHitTestVisible` — for Grid rows that should keep stable height when hidden

### Localization Mapping

WinForms forms use `resources.ApplyResources(this.controlName, "controlName")` which loads properties from `.resx`. In Avalonia, all localizable strings go into `AvPurplePen/UIText.resx` (English) and satellite files (UIText.fr.resx, etc.), referenced via `{resx:Localize}`.

**Mapping resource keys from WinForms .resx to Avalonia UIText.resx:**
- `controlName.Text` in FormName.resx → key `FormName_controlName_Text` in UIText.resx → `{resx:Localize FormName_controlName_Text}` in AXAML
- `$this.Text` in FormName.resx → key `FormName_Text` in UIText.resx → `{resx:Localize FormName_Text}` for window Title
- ComboBox items (`controlName.Items`, `controlName.Items1`, etc.) need different handling since they're often bound to ViewModel collections

### AXAML File Structure

Every ported dialog AXAML should follow this template:
```xml
<!--
    FormNameDialog.axaml
    Brief description of the dialog.
    Migrated from WinForms PurplePen/FormName.cs.
-->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:PurplePen.ViewModels"
        xmlns:resx="using:AvPurplePen"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        d:DesignWidth="..." d:DesignHeight="..."
        x:Class="AvPurplePen.Views.FormNameDialog"
        x:DataType="vm:FormNameDialogViewModel"
        Title="{resx:Localize FormName_Text}"
        Icon="/Assets/transparent.ico"
        Width="..." Height="..."
        CanResize="False"
        CanMinimize="False"
        CanMaximize="False"
        WindowStartupLocation="CenterOwner"
        ShowInTaskbar="False">

    <Design.DataContext>
        <vm:FormNameDialogViewModel/>
    </Design.DataContext>

    <!-- Layout here -->
</Window>
```

### Code-Behind Conventions

- Constructor only calls `InitializeComponent()`. No data initialization.
- DataContext is set by the caller, not the dialog itself.
- Set initial focus in an `Opened` event handler (not the constructor — too early):
  ```csharp
  public MyDialog()
  {
      InitializeComponent();
      Opened += (s, e) => myTextBox.Focus();
  }
  ```
- OK/Cancel button handlers call `Close(true)` / `Close(false)`.
- Use `#if PORTING` blocks for functionality not yet migrated (e.g. sub-dialogs, help system).
- Event handlers that need logic beyond simple Close() are fine in code-behind; move complex business logic to the ViewModel.

### ViewModel Conventions

- Lives in `PurplePenViewModels/` project, namespace `PurplePen.ViewModels`.
- Must have a parameterless constructor for the designer.
- Does NOT contain localized strings — all UI text is in UIText.resx.
- Use `[ObservableProperty]` for bindable properties, `[RelayCommand]` for commands.
- Use `[NotifyPropertyChangedFor(nameof(ComputedProp))]` on a field to auto-raise `PropertyChanged` for dependent computed properties when that field changes. Prefer this over writing `partial void OnXChanged` methods just to call `OnPropertyChanged`.
- Class must be `partial` for source generators to work.

### Validation in ViewModels

ViewModelBase inherits from `ObservableValidator`, which provides `INotifyDataErrorInfo` support. Avalonia's `TextBox` displays validation errors as a red border with the error message in a tooltip (customized in App.axaml — the default Fluent theme shows inline error text below the control, which disrupts layout).

**How to add validation to a field:**
```csharp
// Custom validation (e.g. range checks):
[ObservableProperty]
[NotifyDataErrorInfo]                    // REQUIRED: makes source generator emit ValidateProperty() in setter
[CustomValidation(typeof(MyViewModel), nameof(ValidateMyField))]
private string myField = "";

public static ValidationResult? ValidateMyField(string value, ValidationContext context)
{
    if (string.IsNullOrEmpty(value))
        return ValidationResult.Success;
    if (!float.TryParse(value, out float v) || v < 0 || v > 100)
        return new ValidationResult(MiscText.BadMyField);
    return ValidationResult.Success;
}

// Required field validation (using [Required] attribute):
[ObservableProperty]
[NotifyDataErrorInfo]
[Required(ErrorMessageResourceName = "MyFieldRequired", ErrorMessageResourceType = typeof(MiscText))]
private string myRequiredField = "";
```

**Validation error display:**
App.axaml overrides the default `DataValidationErrors` template to suppress inline error text. Instead, a `TextBox:error` style shows the error message as a tooltip on hover. This keeps dialog layout stable. The red border comes from the Fluent theme's built-in `:error` pseudo-class on TextBox.

**Key rules:**
- `[NotifyDataErrorInfo]` must be on each field that has validation attributes. It is NOT inherited and cannot be placed on the base class. Without it, the source generator does not emit `ValidateProperty()` calls, so validation never fires.
- `[NotifyDataErrorInfo]` must NOT be placed on fields without validation attributes (causes MVVMTK0026 error).
- `[Required]` on a field that starts empty will NOT show an error on dialog open — CommunityToolkit only validates when the setter fires (i.e., when the value changes). So a field starting as `""` won't show an error until the user types and then clears it.
- Only `TextBox.Text` supports inline validation display. `ComboBox.Text` (editable ComboBox) does NOT — its `TextProperty` lacks `enableDataValidation` in metadata. Validate editable ComboBox values in the OK button click handler instead.
- To make an OK button depend on validation state, subscribe to `ErrorsChanged` in the constructor:
  ```csharp
  public bool IsOkEnabled => !string.IsNullOrEmpty(SomeRequiredField) && !HasErrors;

  // In constructor:
  ErrorsChanged += (s, e) => OnPropertyChanged(nameof(IsOkEnabled));
  ```
  `ObservableValidator` does not raise `PropertyChanged` for `HasErrors` automatically, so the manual subscription is needed.

### Porting Workflow

1. Read `FormName.Designer.cs` + `FormName.resx` + `FormName.cs` from the WinForms project.
2. Create `Views/FormNameDialog.axaml` + `.axaml.cs` in AvPurplePen.
3. Create `FormNameDialogViewModel.cs` in PurplePenViewModels (if the dialog needs data binding).
4. Add all localizable strings to `UIText.resx` (and translated variants) using the `FormName_controlName_Text` naming convention.
5. Preserve `x:Name` on controls that will need code-behind or test access.
6. Add a button/menu item in the parent window to launch the dialog, creating and assigning the ViewModel.

### Semi.Avalonia Theme Customization

PurplePen uses the **Semi.Avalonia** theme (HighContrast variant). When customizing control appearance, understanding how the theme's control templates work is essential.

**Finding theme templates:** The Semi.Avalonia source is at `https://github.com/irihitech/Semi.Avalonia`. Control templates are in `src/Semi.Avalonia/Controls/`. For example, `TreeView.axaml` contains both the `TreeView` and `TreeViewItem` control themes. Fetch the raw file from GitHub to read the template XAML and identify template part names, resource keys, and property bindings.

**Style setters vs. resource overrides:** Theme templates often set properties via `{DynamicResource SomeKey}` directly on template elements. These are **local values** in Avalonia's property precedence system and **cannot be overridden by style setters** (styles have lower priority than local values). To override such properties:
- **Override the resource** at a local scope instead of using a style setter:
  ```xml
  <TreeView.Resources>
      <Thickness x:Key="TreeViewItemPadding">0</Thickness>
  </TreeView.Resources>
  ```
- Only use style setters for properties that are NOT set directly in the template.

**Useful Semi.Avalonia template parts:**
- `Button`: `PART_ContentPresenter` (ContentPresenter) — controls border and padding
- `TreeViewItem`: `PART_HeaderPresenter` (ContentPresenter), `PART_ExpandCollapseChevron` (ToggleButton)
- Use `Classes="Small"` on `Button` to get a compact button height (the theme defines size variants this way)

### TreeView with Checkboxes

Avalonia's `TreeView` does not have a built-in `CheckBoxes` property like WinForms. To create a checkbox tree:

1. Create a data model class implementing `INotifyPropertyChanged` with `IsChecked`, `Name`, `Children`, and `Parent` properties.
2. Handle parent↔child checkbox propagation in the model (use a reentrancy guard flag).
3. Use `TreeDataTemplate` with a `CheckBox`:
   ```xml
   <TreeView.ItemTemplate>
       <TreeDataTemplate x:DataType="local:MyNode" ItemsSource="{Binding Children}">
           <CheckBox Content="{Binding Name}" IsChecked="{Binding IsChecked, Mode=TwoWay}"/>
       </TreeDataTemplate>
   </TreeView.ItemTemplate>
   ```

**Always-expanded tree (no collapse):** Use a declarative style, not imperative code. Avalonia virtualizes `TreeViewItem` containers — setting `IsExpanded = true` in code only works once; recycled containers revert to collapsed. Use:
```xml
<TreeView.Styles>
    <Style Selector="TreeViewItem">
        <Setter Property="IsExpanded" Value="True"/>
    </Style>
    <Style Selector="TreeViewItem /template/ ToggleButton#PART_ExpandCollapseChevron">
        <Setter Property="IsVisible" Value="False"/>
    </Style>
</TreeView.Styles>
```

### Rounded Corners on ItemsControls (TreeView, ListBox)

Setting `CornerRadius` directly on a `TreeView` or `ListBox` often fails because the control's internal content (ScrollViewer, ItemsPresenter) paints its background over the rounded corners. Fix by wrapping in a `Border`:

```xml
<Border Background="{DynamicResource InputBackground}"
        BorderBrush="{DynamicResource DarkGreyBorder}"
        BorderThickness="1"
        CornerRadius="3"
        ClipToBounds="True">
    <TreeView Background="Transparent" BorderThickness="0">
        ...
    </TreeView>
</Border>
```

The `Border` owns the visual frame and `ClipToBounds="True"` clips the inner content to the rounded rectangle. The `TreeView` itself must have `Background="Transparent"` and `BorderThickness="0"`.

### Showing Dialogs from ViewModels (DialogService)

Dialogs are shown via `Services.DialogService.ShowDialogAsync(viewModel)`, which resolves the View from the ViewModel type using a naming convention:

- ViewModel: `PurplePen.ViewModels.FooDialogViewModel` (in PurplePenViewModels/)
- View: `AvPurplePen.Views.FooDialog` (in AvPurplePen/Views/Dialogs/)
- Resolution: replaces namespace `PurplePen.ViewModels` → `AvPurplePen.Views`, strips `ViewModel` suffix

**Pattern for command methods:**
```csharp
[RelayCommand(CanExecute = nameof(CanDoSomething))]
private async Task DoSomething()
{
    MyDialogViewModel vm = new MyDialogViewModel { /* set properties */ };
    bool result = await Services.DialogService.ShowDialogAsync(vm);
    if (result) {
        // read results from vm
    }
}
```

**Note on `#if PORTING` / `#if !PORTING`:** The `PORTING` symbol is always defined in AvPurplePen. Use `#if PORTING` for TODO stubs containing only comments (code that will be written later). The `#if !PORTING` blocks in existing code contain old WinForms code to be replaced — when porting a command, replace the entire `#if !PORTING` block with working Avalonia code.

### Porting Custom Controls (UserControls)

When porting WinForms custom controls (not dialogs):
- Create the control as a `UserControl` in `AvPurplePen/Views/` (not in the Dialogs subfolder).
- Custom controls typically use code-behind with direct properties (not a separate ViewModel), similar to `SelectionPanel`.
- Set `ItemsSource` in the constructor code-behind rather than using XAML bindings to the parent control's properties (avoids compiled binding issues with `#root` references).
- If the control needs a data model class (e.g., tree nodes), make it a non-nested public class in the `AvPurplePen` namespace so it can be referenced in AXAML via `x:DataType`.

## Current Development Focus

- **Avalonia port** - AvPurplePen is the new cross-platform application, gradually replacing the WinForms PurplePen project. Code is being moved from PurplePen/ and PurplePenCore/ into AvPurplePen/ (Views) and PurplePenViewModels/ (ViewModels).
- **Skia rendering implementation** - migrating from GDI+ to SkiaSharp for cross-platform support
- **Font fallback** - investigating font rendering issues
- **Layer blending** - fixing template rendering with map files
- **Glyph handling** - fixing OpenMapper import issues with empty glyphs

When working on rendering code, be aware of ongoing Skia migration work.
