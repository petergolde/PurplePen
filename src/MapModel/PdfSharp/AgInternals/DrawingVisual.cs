using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

#if SILVERLIGHT__
namespace System.Windows.Media
{
  /// <summary>
  /// Silverlight 3 version of DrawingVisual.
  /// </summary>
  class DrawingVisual
  {
    //   // Fields
    //private IDrawingContent _content;

    //// Methods
    //public DrawingVisual();
    //internal override void FreeContent(DUCE.Channel channel);
    //internal override Rect GetContentBounds();
    //internal override DrawingGroup GetDrawing();
    //protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters);
    //protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters);
    //internal override void PrecomputeContent();
    //internal override void RenderClose(IDrawingContent newContent);
    //internal override void RenderContent(RenderContext ctx, bool isOnChannel);

    public DrawingContext RenderOpen()
    {
      return new DrawingContext(null);
    }

    //internal override void UpdateRealizations(RealizationContext ctx);
    //internal void WalkContent(DrawingContextWalker walker);

    //// Properties
    //public DrawingGroup Drawing { get; }
  }
}
#endif
