using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabRichTextAlign : Control
    {
        private const int PreviewWidth = 320;
        private const string SampleShort = "Centered text?";
        private const string SampleLong = "This line wraps to show that rich text alignment is applied per wrapped line when the label has extra width available.";

        public TabRichTextAlign()
        {
            TabContainer.SetTabTitle(this, nameof(Tab.RichTextAlign));

            var root = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 8,
                Margin = new Thickness(12)
            };

            root.AddChild(new Label
            {
                Text = "RichText [align=...] testbed. Note: align=fill currently behaves like center (not HTML justify).",
                FontColorOverride = Color.White
            });

            root.AddChild(CreateSection(
                "Auto width (looks like align does nothing because the label shrinks to content)",
                fixedWidth: false));

            root.AddChild(CreateSection(
                $"Fixed width ({PreviewWidth}px, alignment becomes visible)",
                fixedWidth: true));

            AddChild(new ScrollContainer
            {
                Children = { root },
                VScrollEnabled = true,
                HScrollEnabled = false
            });
        }

        private static Control CreateSection(string title, bool fixedWidth)
        {
            var box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 4
            };

            box.AddChild(new Label
            {
                Text = title,
                FontColorOverride = Color.FromHex("#E9F2FF")
            });

            box.AddChild(CreatePreviewPanel("[align=\"left\"]left[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[align=\"center\"]center[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[align=\"right\"]right[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[align=\"fill\"]fill (currently centered)[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel($"[align=\"center\"]{SampleShort}[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel($"[align=\"right\"]{SampleShort}[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel($"[align=\"center\"]{SampleLong}[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel($"[align=\"right\"]{SampleLong}[/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[tracking=2]Tracking +2[/tracking]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[tracking=-1]Tracking -1[/tracking]", fixedWidth));
            box.AddChild(CreatePreviewPanel("[align=\"center\"][tracking=2]Centered with tracking[/tracking][/align]", fixedWidth));
            box.AddChild(CreatePreviewPanel(
                $"Before [color=yellow][align=\"center\"]{SampleLong}[/align][/color] After (tag scoped mid-message)",
                fixedWidth));

            return new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#223146"),
                    BorderColor = Color.FromHex("#0F1824"),
                    BorderThickness = new Thickness(2),
                    ContentMarginLeftOverride = 8,
                    ContentMarginTopOverride = 8,
                    ContentMarginRightOverride = 8,
                    ContentMarginBottomOverride = 8
                },
                Children = { box }
            };
        }

        private static Control CreatePreviewPanel(string markup, bool fixedWidth)
        {
            var label = new RichTextLabel
            {
                Margin = new Thickness(2),
                MaxWidth = PreviewWidth
            };

            if (fixedWidth)
                label.SetWidth = PreviewWidth;

            label.SetMessage(FormattedMessage.FromMarkupOrThrow($"[and o]{markup}[/color]"), tagsAllowed: null);

            return new PanelContainer
            {
                MinWidth = PreviewWidth,
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#101722"),
                    BorderColor = Color.FromHex("#465A73"),
                    BorderThickness = new Thickness(1),
                    ContentMarginLeftOverride = 6,
                    ContentMarginTopOverride = 4,
                    ContentMarginRightOverride = 6,
                    ContentMarginBottomOverride = 4
                },
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        Children =
                        {
                            new Label
                            {
                                Text = markup,
                                FontColorOverride = Color.FromHex("#9FB3C8"),
                                ClipText = true
                            },
                            label
                        }
                    }
                }
            };
        }
    }
}
