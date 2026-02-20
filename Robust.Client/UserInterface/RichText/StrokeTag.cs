using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Renders text with a stroked outline.
/// </summary>
public sealed class StrokeTag : IMarkupTagHandler
{
    public string Name => "stroke";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var color = ResolveColor(node, Color.Black);
        var thickness = ResolveThicknessAttribute(node, "thickness", 1f);
        var lineCap = ResolveLineCapAttribute(node, "linecap", FontStrokeLineCap.Round);
        var lineJoin = ResolveLineJoinAttribute(node, "linejoin", FontStrokeLineJoin.Round);

        context.StrokeColor.Push(color);
        context.StrokeThickness.Push(Math.Max(thickness, 0));
        context.StrokeLineCap.Push(lineCap);
        context.StrokeLineJoin.Push(lineJoin);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.StrokeLineJoin.Pop();
        context.StrokeLineCap.Pop();
        context.StrokeThickness.Pop();
        context.StrokeColor.Pop();
    }

    private static Color ResolveColor(MarkupNode node, Color fallback)
    {
        if (node.Value.TryGetColor(out var color)
            || node.Attributes.TryGetValue("color", out var colorAttr)
            && colorAttr.TryGetColor(out color))
            return color.Value;

        return fallback;
    }

    private static float ResolveThicknessAttribute(MarkupNode node, string name, float fallback)
    {
        if (!node.Attributes.TryGetValue(name, out var attr))
            return fallback;

        if (attr.TryGetLong(out var whole))
            return whole.Value;

        if (attr.TryGetString(out var text) && Parse.TryFloat(text, out var value))
            return value;

        return fallback;
    }

    private static FontStrokeLineCap ResolveLineCapAttribute(MarkupNode node, string name, FontStrokeLineCap fallback)
    {
        if (!node.Attributes.TryGetValue(name, out var attr) || !attr.TryGetString(out var value))
            return fallback;

        return value.ToLowerInvariant() switch
        {
            "butt" => FontStrokeLineCap.Butt,
            "square" => FontStrokeLineCap.Square,
            "round" => FontStrokeLineCap.Round,
            _ => fallback
        };
    }

    private static FontStrokeLineJoin ResolveLineJoinAttribute(MarkupNode node, string name, FontStrokeLineJoin fallback)
    {
        if (!node.Attributes.TryGetValue(name, out var attr) || !attr.TryGetString(out var value))
            return fallback;

        return value.ToLowerInvariant() switch
        {
            "bevel" => FontStrokeLineJoin.Bevel,
            "miter" => FontStrokeLineJoin.Miter,
            "round" => FontStrokeLineJoin.Round,
            _ => fallback
        };
    }
}
