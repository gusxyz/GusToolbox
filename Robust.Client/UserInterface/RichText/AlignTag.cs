using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class AlignTag : IMarkupTagHandler
{
    public string Name => "align";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var fallback = context.Align.TryPeek(out var existing)
            ? existing
            : Label.AlignMode.Left;

        context.Align.Push(ParseAlign(node, fallback));
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Align.Pop();
    }

    private static Label.AlignMode ParseAlign(MarkupNode node, Label.AlignMode fallback)
    {
        if (!TryGetAlign(node, out var align))
            return fallback;

        return align.Trim().ToLowerInvariant() switch
        {
            "left" => Label.AlignMode.Left,
            "center" => Label.AlignMode.Center,
            "right" => Label.AlignMode.Right,
            "fill" => Label.AlignMode.Fill,
            _ => fallback
        };
    }

    private static bool TryGetAlign(MarkupNode node, out string align)
    {
        if (node.Value.TryGetString(out var value) && !string.IsNullOrWhiteSpace(value))
        {
            align = value;
            return true;
        }

        if (node.Attributes.TryGetValue("mode", out var modeAttr)
            && modeAttr.TryGetString(out value)
            && !string.IsNullOrWhiteSpace(value))
        {
            align = value;
            return true;
        }

        if (node.Attributes.TryGetValue("align", out var alignAttr)
            && alignAttr.TryGetString(out value)
            && !string.IsNullOrWhiteSpace(value))
        {
            align = value;
            return true;
        }

        align = string.Empty;
        return false;
    }
}
