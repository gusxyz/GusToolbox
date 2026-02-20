using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Renders text with a drop shadow.
/// </summary>
public sealed class DropShadowTag : IMarkupTagHandler
{
    public string Name => "dropshadow";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var color = ResolveColor(node, Color.Black.WithAlpha(0.8f));
        var x = ResolveIntAttribute(node, "x", 1);
        var y = ResolveIntAttribute(node, "y", 1);

        context.ShadowColor.Push(color);
        context.ShadowOffset.Push(new Vector2(x, y));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.ShadowOffset.Pop();
        context.ShadowColor.Pop();
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
