using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupDrawingContext
{
    public readonly Stack<Color> Color;
    public readonly Stack<Font> Font;
    public readonly Stack<Color> ShadowColor;
    public readonly Stack<Vector2> ShadowOffset;
    public readonly Stack<Color> StrokeColor;
    public readonly Stack<float> StrokeThickness;
    public readonly Stack<FontStrokeLineCap> StrokeLineCap;
    public readonly Stack<FontStrokeLineJoin> StrokeLineJoin;
    public readonly Stack<Label.AlignMode> Align;
    public readonly Stack<float> Tracking;
    public readonly List<IMarkupTag> Tags;

    public MarkupDrawingContext()
    {
        Color = new Stack<Color>();
        Font = new Stack<Font>();
        ShadowColor = new Stack<Color>();
        ShadowOffset = new Stack<Vector2>();
        StrokeColor = new Stack<Color>();
        StrokeThickness = new Stack<float>();
        StrokeLineCap = new Stack<FontStrokeLineCap>();
        StrokeLineJoin = new Stack<FontStrokeLineJoin>();
        Align = new Stack<Label.AlignMode>();
        Tracking = new Stack<float>();
        Tags = new List<IMarkupTag>();
    }

    public MarkupDrawingContext(int capacity)
    {
        Color = new Stack<Color>(capacity);
        Font = new Stack<Font>(capacity);
        ShadowColor = new Stack<Color>(capacity);
        ShadowOffset = new Stack<Vector2>(capacity);
        StrokeColor = new Stack<Color>(capacity);
        StrokeThickness = new Stack<float>(capacity);
        StrokeLineCap = new Stack<FontStrokeLineCap>(capacity);
        StrokeLineJoin = new Stack<FontStrokeLineJoin>(capacity);
        Align = new Stack<Label.AlignMode>(capacity);
        Tracking = new Stack<float>(capacity);
        Tags = new List<IMarkupTag>();
    }

    public void Clear()
    {
        Color.Clear();
        Font.Clear();
        ShadowColor.Clear();
        ShadowOffset.Clear();
        StrokeColor.Clear();
        StrokeThickness.Clear();
        StrokeLineCap.Clear();
        StrokeLineJoin.Clear();
        Align.Clear();
        Tracking.Clear();
        Tags.Clear();
    }
}
