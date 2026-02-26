using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Adds letter spacing (tracking) in pixels to glyph advance while active.
/// Example: [tracking=2]WIDE[/tracking]
/// </summary>
public sealed class TrackingTag : IMarkupTagHandler
{
    public string Name => "tracking";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var fallback = context.Tracking.TryPeek(out var existing)
            ? existing
            : 0f;

        context.Tracking.Push(ResolveTracking(node, fallback));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) => context.Tracking.Pop();

    private static float ResolveTracking(MarkupNode node, float fallback)
    {
        if (TryGetNumber(node.Value, out var value)
            || node.Attributes.TryGetValue("amount", out var amount)
            && TryGetNumber(amount, out value) || node.Attributes.TryGetValue("value", out var attrValue)
            && TryGetNumber(attrValue, out value) || node.Attributes.TryGetValue("tracking", out var tracking)
            && TryGetNumber(tracking, out value))
            return value;

        return fallback;
    }

    private static bool TryGetNumber(MarkupParameter param, out float value)
    {
        if (param.TryGetLong(out var whole))
        {
            value = whole.Value;
            return true;
        }

        if (param.TryGetString(out var text) && Parse.TryFloat(text, out value))
            return true;

        value = default;
        return false;
    }
}
