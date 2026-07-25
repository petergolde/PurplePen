using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Rendering;
using Avalonia.Styling;
using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using PurplePen.ViewModels;
using SkiaSharp;
using System;
using System.Drawing;
using System.Windows.Input;

namespace AvPurplePen;

public partial class DescriptionViewer : UserControl
{
    public static readonly StyledProperty<DescriptionData?> DescriptionDataProperty =
        AvaloniaProperty.Register<DescriptionViewer, DescriptionData?>(nameof(DescriptionData));

    public static readonly StyledProperty<SelectedLines?> SelectionProperty =
        AvaloniaProperty.Register<DescriptionViewer, SelectedLines?>(nameof(Selection), defaultBindingMode: BindingMode.TwoWay);

    // Command invoked when the user selects an item or enters text in a description popup.
    // The command parameter is a DescriptionChangeCommandData instance.
    public static readonly StyledProperty<ICommand?> DescriptionChangeCommandProperty =
        AvaloniaProperty.Register<DescriptionViewer, ICommand?>(nameof(DescriptionChangeCommand));

    private SymbolDB? symbolDB;
    private DescriptionRenderer? renderer;

    // This is the flyout for the popup menu, if it is open. 
    private Flyout? flyout;

    private const int margin = 3;            // margin size in logical pixels
    private const int minCellSize = 20;      // minimum cell size in logical pixels

    public DescriptionViewer()
    {
        InitializeComponent();
        drawingView.Paint += DrawingView_Paint;
        drawingView.PointerPressed += DrawingView_PointerPressed;
        UpdateView();
    }

    public DescriptionData? DescriptionData
    {
        get => GetValue(DescriptionDataProperty);
        set => SetValue(DescriptionDataProperty, value);
    }

    public delegate void DescriptionChangedHandler(object sender, DescriptionChangeKind kind, int line, int box, object newValue);

    // Indicates which line(s) are selected, or null for
    // nothing selected.
    public SelectedLines? Selection {
        get => GetValue(SelectionProperty);
        set => SetValue(SelectionProperty, value);
    }

    // Command invoked when the user selects an item or enters text in a description popup.
    // The command parameter is a DescriptionChangeCommandData instance.
    public ICommand? DescriptionChangeCommand {
        get => GetValue(DescriptionChangeCommandProperty);
        set => SetValue(DescriptionChangeCommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DescriptionDataProperty && drawingView is not null) {
            DescriptionDataUpdated();
        }
        else if (change.Property == SelectionProperty && drawingView is not null) {
            UpdateView();
        }
        else if (change.Property == BoundsProperty && drawingView is not null) {
            UpdateView();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (this.DataContext is DescriptionViewerViewModel vm) {
            if (vm.SymbolDB != null) {
                // Create the renderer.
                symbolDB = vm.SymbolDB;
                renderer = new DescriptionRenderer(vm.SymbolDB);
                renderer.Margin = margin;
                renderer.DescriptionKind = DescriptionKind.Symbols;     // control always shows symbols.
            }
        }
    }

    // Called when DescriptionData updates.
    private void DescriptionDataUpdated()
    {
        if (Selection != null) {
            if (DescriptionData == null || DescriptionData.Description == null) {
                Selection = null;
            }
            else {
                int maxLine = DescriptionData.Description.Length - 1;
                if (Selection.FirstLine > maxLine || Selection.LastLine > maxLine) {
                    Selection = null;
                }
            }
        }

        UpdateView();
    }

    // The mouse was clicked on the description.
    private void DrawingView_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (renderer == null)
            return;

        PointerPoint pointerPoint = drawingView.GetPointerPoint(e);

   
        HitTestResult hitTest = renderer.HitTest(Conv.ToPointF(pointerPoint.Position));
        if (hitTest.firstLine < 0)
            return;             // clicked outside the description.

        bool alreadySelected = (Selection != null && hitTest.firstLine == Selection.FirstLine);

        if (!alreadySelected) {
            // Move the selected line.
            Selection = new SelectedLines(hitTest.firstLine, hitTest.lastLine);
        }

        PointerUpdateKind whichButton = pointerPoint.Properties.PointerUpdateKind;

        // If the left-click the selected line, or right-click anywhere, then possibly show a popup menu.
        if ((whichButton == PointerUpdateKind.RightButtonPressed || alreadySelected) && hitTest.kind != HitTestKind.None) {
            // Clicked on the selected line, in a potentially interesting place. Show a menu (maybe).
            PopupMenu(hitTest);
        }
    }

    // Calculates the content area inside a popup button cell in physical pixels.
    // Uses the CELLSIZE and BUTTON_CHROME_PER_SIDE constants from DescriptionPopup,
    // and does the subtraction in physical pixel space with AwayFromZero rounding
    // to match Avalonia's layout rounding (LayoutHelper.RoundLayoutValue).
    private int MeasureCellContentSize()
    {
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        int physicalCell = (int)Math.Round(DescriptionPopup.CELLSIZE * scaling, MidpointRounding.AwayFromZero);
        int physicalPerSide = (int)Math.Round(DescriptionPopup.BUTTON_CHROME_PER_SIDE * scaling, MidpointRounding.AwayFromZero);
        int pixelSize = physicalCell - 2 * physicalPerSide;
        return pixelSize;
    }

    private void PopupMenu(HitTestResult hitTest)
    {
        if (renderer == null || symbolDB == null || DataContext == null)
            return;

        DescriptionViewerViewModel? vm = DataContext as DescriptionViewerViewModel;
        if (vm == null)
            return;

        int cellContentPixelSize = MeasureCellContentSize();

        // Get the information to configure the popup menu, or null if no popup menu should be shown.
        DescriptionPopupViewModel? popupViewModel = vm.GetPopupMenu(hitTest, renderer, cellContentPixelSize);
        if (popupViewModel == null) 
            return;

        Avalonia.Point popupMenuLocation = new Avalonia.Point(hitTest.rect.Left + renderer.CellSize * 0.5F,
                                                              hitTest.rect.Top + renderer.CellSize * 0.75F);
        popupMenuLocation -= drawingView.Offset;

        DescriptionPopup descriptionPopup = new() {
            DataContext = popupViewModel
        };

        flyout = new Flyout() {
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.TopLeft,
            PlacementGravity = PopupGravity.BottomRight,
            HorizontalOffset = popupMenuLocation.X,
            VerticalOffset = popupMenuLocation.Y,
            FlyoutPresenterTheme = new ControlTheme(typeof(FlyoutPresenter)) {
                BasedOn = (ControlTheme)this.FindResource(typeof(FlyoutPresenter))!,
                Setters = { new Setter(FlyoutPresenter.PaddingProperty, new Thickness(3)), 
                            new Setter(FlyoutPresenter.BackgroundProperty, new SolidColorBrush(Avalonia.Media.Color.FromRgb(0xFB, 0xF8, 0xED))) }
            },
            Content = descriptionPopup
        };

        descriptionPopup.PopupItemSelected += DescriptionPopup_PopupItemSelected;
        flyout.Closed += Flyout_Closed;
        flyout.ShowAt(drawingView);
    }

    // The flyout closed. Don't track it anymore.
    private void Flyout_Closed(object? sender, EventArgs e)
    {
        flyout = null;
    }

    // The popup selected an item or modified text.
    // Close the flyout, and invoke a command to make the change.
    private void DescriptionPopup_PopupItemSelected(object? sender, PopupItemSelectedEventArgs e)
    {
        flyout?.Hide();
        flyout = null;

        object? newValue = e.NewSymbol != null ? (object)e.NewSymbol : e.NewText;
        DescriptionChangeCommandData data = new DescriptionChangeCommandData(
            e.DescriptionChangeKind, e.ChangedLine, e.ChangedBox, newValue);

        if (DescriptionChangeCommand != null && DescriptionChangeCommand.CanExecute(data)) {
            DescriptionChangeCommand.Execute(data);
        }
    }



    // Called when something requires the view to be redrawn.
    private void UpdateView()
    {
        if (renderer != null && drawingView  != null && DescriptionData != null) { 
            Avalonia.Size size = Bounds.Size;

            if (DescriptionData.Description != null) {
                renderer.Description = DescriptionData.Description;
            }
            else {
                renderer.Description = new DescriptionLine[0];
            }

            float boxSize = Math.Max(minCellSize, (float)(size.Width - 0.1 - (margin * 2)) / 8F);
            renderer.CellSize = boxSize;
            SizeF descriptionSize = renderer.Measure();

            if (descriptionSize.Height > size.Height - 1) {
                // Gonna need a scroll bar. Resize again.
                int scrollBarWidth = 8;  // I happen to know that's the width of a scroll bar in my theme.
                boxSize = Math.Max(minCellSize, (float)(size.Width - 0.1 - scrollBarWidth - (margin * 2)) / 8F);
                renderer.CellSize = boxSize;
                descriptionSize = renderer.Measure();
            }

            drawingView.LogicalExtent = Conv.ToAvSize(descriptionSize);

            drawingView.InvalidateSurface();
        }
    }

    private void DrawingView_Paint(object? sender, SkiaScrollableDrawingView.PaintEventArgs e)
    {
        RectangleF clipRectangle = Conv.ToRectangleF(e.LogicalViewPort);
        e.Canvas.Clear(SKColors.White);

        if (renderer != null && DescriptionData != null) {
            using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(e.Canvas)) {
                renderer.Description = DescriptionData.Description;

                grTarget.PushAntiAliasing(true);
                DrawSelection(grTarget, clipRectangle);
                renderer.RenderToGraphics(grTarget, clipRectangle);
                grTarget.PopAntiAliasing();
            }
        }
    }

    private void DrawSelection(IGraphicsTarget grTarget, RectangleF clipRectangle)
    {
        if (renderer != null && Selection != null && DescriptionData != null && DescriptionData.Description != null &&
            Selection.FirstLine >= 0 && Selection.LastLine >= 0 &&
            Selection.FirstLine < DescriptionData.Description.Length && Selection.LastLine < DescriptionData.Description.Length) 
        {
            RectangleF selectedRect = (renderer.LineBounds(Selection.FirstLine, Selection.LastLine));
            if (selectedRect.IntersectsWith(clipRectangle)) {
                object selectionBrush = new object();
                grTarget.CreateSolidBrush(selectionBrush, CmykColor.FromColor(System.Drawing.Color.Yellow));
                grTarget.FillRectangle(selectionBrush, selectedRect);
            }
        }
    }
}




