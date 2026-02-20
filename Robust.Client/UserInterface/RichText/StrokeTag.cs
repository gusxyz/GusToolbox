using System;
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
        var thickness = ResolveIntAttribute(node, "thickness", 1);

        context.StrokeColor.Push(color);
        context.StrokeThickness.Push(Math.Max(thickness, 0));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.StrokeThickness.Pop();
        context.StrokeColor.Pop();
    }

    private static Color ResolveColor(MarkupNode node, Color fallback)
    {
        if (node.Value.TryGetColor(out var color))
            return color.Value;

        if (node.Attributes.TryGetValue("color", out var colorAttr) && colorAttr.TryGetColor(out color))
            return color.Value;

        return fallback;
    }

    private static int ResolveIntAttribute(MarkupNode node, string name, int fallback)
    {
        if (!node.Attributes.TryGetValue(name, out var attr) || !attr.TryGetLong(out var value))
            return fallback;

        return (int) value.Value;
    }
}
