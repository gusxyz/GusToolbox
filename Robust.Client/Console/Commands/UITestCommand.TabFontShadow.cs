using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabFontShadow : Control
    {
        public TabFontShadow()
        {
            TabContainer.SetTabTitle(this, nameof(Tab.FontShadow));

            var runescapeFont = LoadRunescapeFont(16);
            var previewText = "THE QUICK BROWN FOX 0123456789";

            var heading = CreatePreviewLabel(runescapeFont);
            heading.SetMessage(
                FormattedMessage.FromMarkupOrThrow(
                    "[color=white][stroke=#161616 thickness=1][dropshadow=#000000CC x=1 y=1]Font Stroke + Drop Shadow Preview[/dropshadow][/stroke][/color]"));

            var plain = CreatePreviewLabel(runescapeFont);
            var shadow = CreatePreviewLabel(runescapeFont);
            var stroke = CreatePreviewLabel(runescapeFont);
            var combo = CreatePreviewLabel(runescapeFont);

            var strokeSlider = new Slider
            {
                MinValue = 0,
                MaxValue = 4,
                Value = 2,
                Rounded = true,
                MinHeight = 24
            };

            var shadowXSlider = new Slider
            {
                MinValue = -4,
                MaxValue = 8,
                Value = 2,
                Rounded = true,
                MinHeight = 24
            };

            var shadowYSlider = new Slider
            {
                MinValue = -4,
                MaxValue = 8,
                Value = 2,
                Rounded = true,
                MinHeight = 24
            };

            var sliderInfo = new Label
            {
                FontOverride = runescapeFont,
                FontColorOverride = Color.White
            };

            strokeSlider.OnValueChanged += _ => UpdatePreview();
            shadowXSlider.OnValueChanged += _ => UpdatePreview();
            shadowYSlider.OnValueChanged += _ => UpdatePreview();

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 6,
                Margin = new Thickness(12),
                Children =
                {
                    heading,
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = Color.FromHex("#3d4f67"),
                            BorderColor = Color.FromHex("#19222f"),
                            BorderThickness = new Thickness(2),
                            ContentMarginLeftOverride = 8,
                            ContentMarginTopOverride = 8,
                            ContentMarginRightOverride = 8,
                            ContentMarginBottomOverride = 8,
                        },
                        Children =
                        {
                            new BoxContainer
                            {
                                Orientation = BoxContainer.LayoutOrientation.Vertical,
                                SeparationOverride = 4,
                                Children =
                                {
                                    plain,
                                    shadow,
                                    stroke,
                                    combo,
                                }
                            }
                        }
                    },
                    sliderInfo,
                    new Label
                    {
                        Text = "Stroke Thickness",
                        FontOverride = runescapeFont,
                        FontColorOverride = Color.White
                    },
                    strokeSlider,
                    new Label
                    {
                        Text = "Shadow Offset X",
                        FontOverride = runescapeFont,
                        FontColorOverride = Color.White
                    },
                    shadowXSlider,
                    new Label
                    {
                        Text = "Shadow Offset Y",
                        FontOverride = runescapeFont,
                        FontColorOverride = Color.White
                    },
                    shadowYSlider,
                }
            });

            UpdatePreview();

            void UpdatePreview()
            {
                var strokeThickness = (int) strokeSlider.Value;
                var shadowX = (int) shadowXSlider.Value;
                var shadowY = (int) shadowYSlider.Value;

                plain.SetMessage(FormattedMessage.FromMarkupOrThrow($"[color=#f5f5f5]Plain: {previewText}[/color]"));
                shadow.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#f6f6f6][dropshadow=#000000D9 x={shadowX} y={shadowY}]Shadow: {previewText}[/dropshadow][/color]"));
                stroke.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#fff6d6][stroke=#1a1a1a thickness={strokeThickness}]Stroke: {previewText}[/stroke][/color]"));
                combo.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#f9fff0][stroke=#182538 thickness={strokeThickness}][dropshadow=#000000D9 x={shadowX} y={shadowY}]Stroke + Shadow: {previewText}[/dropshadow][/stroke][/color]"));
                sliderInfo.Text = $"Interactive settings: stroke={strokeThickness}px shadow=({shadowX}, {shadowY})";
            }
        }

        private static RichTextLabel CreatePreviewLabel(Font? font)
        {
            var label = new RichTextLabel();

            if (font != null)
            {
                label.Stylesheet = new Stylesheet([
                    StylesheetHelpers.Element<RichTextLabel>().Prop("font", font)
                ]);
            }

            return label;
        }

        private static Font? LoadRunescapeFont(int size)
        {
            var systemFontManager = IoCManager.Resolve<ISystemFontManager>();
            if (!systemFontManager.IsSupported)
                return null;

            foreach (var fontFace in systemFontManager.SystemFontFaces)
            {
                if (!fontFace.PostscriptName.Contains("runescape", StringComparison.OrdinalIgnoreCase) &&
                    !fontFace.FullName.Contains("runescape", StringComparison.OrdinalIgnoreCase) &&
                    !fontFace.FamilyName.Contains("runescape", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return fontFace.Load(size);
            }

            return null;
        }
    }
}
