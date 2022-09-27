using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PdfSharp.Drawing;

#if SILVERLIGHT
namespace System.Windows.Media
{
  /// <summary>
  /// Silverlight 3 version of DrawingContext.
  /// </summary>
  public class DrawingContext
  {
    internal DrawingContext(Canvas canvas)
    {
      if (canvas == null)
        throw new ArgumentNullException("canvas");

      _canvas = canvas;

      // Init stack with identity matrix
      _matrixStack.Push(new XMatrix());

#if DEBUG
      Rectangle rectange = new Rectangle();
      rectange.Width = 100;
      rectange.Height = 50;
      rectange.Fill = new SolidColorBrush { Color = Colors.Red };
      Canvas.SetLeft(rectange, 50);
      Canvas.SetTop(rectange, 70);
      _canvas.Children.Add(rectange);
#endif
    }

    public void Close()
    {

    }

    //public void DrawDrawing(Drawing drawing);

    public void DrawEllipse(Brush brush, Pen pen, Point center, double radiusX, double radiusY)
    {
      Ellipse ellipse = new Ellipse();
      SetupShape(ellipse, center.X - radiusX, center.Y - radiusY, radiusX * 2, radiusY * 2, brush, pen);
      ellipse.Fill = brush;
      _canvas.Children.Add(ellipse);
    }

    //public void DrawEllipse(Brush brush, Pen pen, Point center, AnimationClock centerAnimations, double radiusX, AnimationClock radiusXAnimations, double radiusY, AnimationClock radiusYAnimations);

    public void DrawGeometry(Brush brush, Pen pen, Geometry geometry)
    {
      //geometry.
    }

    //public void DrawGlyphRun(Brush foregroundBrush, GlyphRun glyphRun);

    public void DrawImage(ImageSource imageSource, Rect rectangle)
    {
    }

    public void DrawLine(Pen pen, Point point0, Point point1)
    {
      Line line = new Line();
      SetupShape(line, point0.X, point0.Y, point1.X - point0.X, point1.Y - point0.Y, null, pen);
      line.X1 = point0.X;
      line.Y1 = point0.Y;
      line.X2 = point1.X;
      line.Y2 = point1.Y;
      _canvas.Children.Add(line);
    }

    public void DrawRectangle(Brush brush, Pen pen, Rect rectangle)
    {
    }

    public void DrawRoundedRectangle(Brush brush, Pen pen, Rect rectangle, double radiusX, double radiusY)
    {
    }

    static void SetupShape(Shape shape, double x, double y, double width, double height, Brush brush, Pen pen)
    {
      Canvas.SetLeft(shape, x);
      Canvas.SetTop(shape, y);
      shape.Width = width;
      shape.Height = height;
      shape.Fill = brush;
      if (pen != null)
      {
        shape.Stroke = pen.Brush;
        shape.StrokeThickness = pen.Thickness;
      }
    }

    //public void DrawRoundedRectangle(Brush brush, Pen pen, Rect rectangle, AnimationClock rectangleAnimations, double radiusX, AnimationClock radiusXAnimations, double radiusY, AnimationClock radiusYAnimations);
    //public void DrawText(FormattedText formattedText, Point origin);
    //public void DrawVideo(MediaPlayer player, Rect rectangle);
    //public void DrawVideo(MediaPlayer player, Rect rectangle, AnimationClock rectangleAnimations);

    public void Pop()
    { }

    public void PushClip(Geometry clipGeometry)
    {
    }

    //public void PushEffect(BitmapEffect effect, BitmapEffectInput effectInput);
    //public void PushGuidelineSet(GuidelineSet guidelines);
    //public void PushOpacity(double opacity);
    //public void PushOpacity(double opacity, AnimationClock opacityAnimations);
    //public void PushOpacityMask(Brush opacityMask);

    public void PushTransform(MatrixTransform transform)
    {
      XMatrix matrix = _matrixStack.Peek();
      matrix.Append(transform.Matrix);
    }

    public void DrawString(XGraphics gfx, string text, XFont font, XBrush brush, XRect layoutRectangle, XStringFormat format)
    {
      double x = layoutRectangle.X;
      double y = layoutRectangle.Y;

      double lineSpace = font.GetHeight(gfx);
      double cyAscent = lineSpace * font.cellAscent / font.cellSpace;
      double cyDescent = lineSpace * font.cellDescent / font.cellSpace;

      bool bold = (font.Style & XFontStyle.Bold) != 0;
      bool italic = (font.Style & XFontStyle.Italic) != 0;
      bool strikeout = (font.Style & XFontStyle.Strikeout) != 0;
      bool underline = (font.Style & XFontStyle.Underline) != 0;

      //FormattedText formattedText = new FormattedText(text, new CultureInfo("en-us"), // WPFHACK
      //  FlowDirection.LeftToRight, font.typeface, font.Size, brush.RealizeWpfBrush());
      TextBlock textBlock = FontHelper.CreateTextBlock(text, null, font.Size, brush.RealizeWpfBrush());

      Canvas.SetLeft(textBlock, x);
      Canvas.SetTop(textBlock, y);

      //formattedText.SetTextDecorations(TextDecorations.OverLine);
      switch (format.Alignment)
      {
        case XStringAlignment.Near:
          // nothing to do, this is the default
          //formattedText.TextAlignment = TextAlignment.Left;
          break;

        case XStringAlignment.Center:
          x += layoutRectangle.Width / 2;
          textBlock.TextAlignment = TextAlignment.Center;
          break;

        case XStringAlignment.Far:
          x += layoutRectangle.Width;
          textBlock.TextAlignment = TextAlignment.Right;
          break;
      }
      if (gfx.PageDirection == XPageDirection.Downwards)
      {
        switch (format.LineAlignment)
        {
          case XLineAlignment.Near:
            //y += cyAscent;
            break;

          //case XLineAlignment.Center:
          //  // TODO use CapHeight. PDFlib also uses 3/4 of ascent
          //  y += -formattedText.Baseline + (cyAscent * 1 / 3) + layoutRectangle.Height / 2;
          //  //y += -formattedText.Baseline + (font.Size * font.Metrics.CapHeight / font.unitsPerEm / 2) + layoutRectangle.Height / 2;
          //  break;

          //case XLineAlignment.Far:
          //  y += -formattedText.Baseline - cyDescent + layoutRectangle.Height;
          //  break;

          //case XLineAlignment.BaseLine:
          //  y -= formattedText.Baseline;
          //  break;
        }
      }
      else
      {
        // TODOWPF: make unit test
        switch (format.LineAlignment)
        {
          case XLineAlignment.Near:
            //y += cyDescent;
            break;

          case XLineAlignment.Center:
            // TODO use CapHeight. PDFlib also uses 3/4 of ascent
            //y += -(cyAscent * 3 / 4) / 2 + rect.Height / 2;
            break;

          case XLineAlignment.Far:
            //y += -cyAscent + rect.Height;
            break;

          case XLineAlignment.BaseLine:
            // nothing to do
            break;
        }
      }

      //if (bold && !descriptor.IsBoldFace)
      //{
      //  // TODO: emulate bold by thicker outline
      //}

      //if (italic && !descriptor.IsBoldFace)
      //{
      //  // TODO: emulate italic by shearing transformation
      //}

      //if (underline)
      //{
      //  formattedText.FontStyle.SetTextDecorations(TextDecorations.Underline);
      //  //double underlinePosition = lineSpace * realizedFont.FontDescriptor.descriptor.UnderlinePosition / font.cellSpace;
      //  //double underlineThickness = lineSpace * realizedFont.FontDescriptor.descriptor.UnderlineThickness / font.cellSpace;
      //  //DrawRectangle(null, brush, x, y - underlinePosition, width, underlineThickness);
      //}

      //if (strikeout)
      //{
      //  formattedText.SetTextDecorations(TextDecorations.Strikethrough);
      //  //double strikeoutPosition = lineSpace * realizedFont.FontDescriptor.descriptor.StrikeoutPosition / font.cellSpace;
      //  //double strikeoutSize = lineSpace * realizedFont.FontDescriptor.descriptor.StrikeoutSize / font.cellSpace;
      //  //DrawRectangle(null, brush, x, y - strikeoutPosition - strikeoutSize, width, strikeoutSize);
      //}

      //formattedText 
      _canvas.Children.Add(textBlock);
    }

    public XSize MeasureString(XGraphics gfx, string text, XFont font, XStringFormat stringFormat)
    {
      TextBlock textBlock = FontHelper.CreateTextBlock(text, null, font.Size, null);
      return new XSize(textBlock.ActualWidth, textBlock.ActualHeight);
    }

    Canvas _canvas = new Canvas();

    readonly Stack<XMatrix> _matrixStack = new Stack<XMatrix>();
  }

  public sealed class Pen //: Animatable
  {
    //public Pen();

    public Pen(Brush brush, double thickness)
    {
      Brush = brush;
      Thickness = thickness;
    }

    public Brush Brush { get; set; }
    public double Thickness { get; set; }
    //public DashStyle DashStyle { get; set; }
    public double[] DashArray { get; set; }
    public double DashOffset { get; set; }
    public PenLineCap StartLineCap { get; set; }
    public PenLineCap EndLineCap { get; set; }
    public PenLineCap DashCap { get; set; }
    public PenLineJoin LineJoin { get; set; }
    public double MiterLimit { get; set; }

    //public Pen Clone();
    //public Pen CloneCurrentValue();
  }
}
#endif