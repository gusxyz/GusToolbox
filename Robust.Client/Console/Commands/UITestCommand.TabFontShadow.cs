using System;
using System.Globalization;
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
            var previewText = "T H E   Q U I C K   B R O W N   F O X   0 1 2 3 4 5 6 7 8 9";

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
                Value = 1.5f,
                Rounded = true,
                RoundingDecimals = 2,
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

            var lineCapButton = new OptionButton();
            lineCapButton.AddItem("Round", (int) FontStrokeLineCap.Round);
            lineCapButton.AddItem("Butt", (int) FontStrokeLineCap.Butt);
            lineCapButton.AddItem("Square", (int) FontStrokeLineCap.Square);
            lineCapButton.SelectId((int) FontStrokeLineCap.Round);

            var lineJoinButton = new OptionButton();
            lineJoinButton.AddItem("Round", (int) FontStrokeLineJoin.Round);
            lineJoinButton.AddItem("Bevel", (int) FontStrokeLineJoin.Bevel);
            lineJoinButton.AddItem("Miter", (int) FontStrokeLineJoin.Miter);
            lineJoinButton.SelectId((int) FontStrokeLineJoin.Round);

            var sliderInfo = new Label
            {
                FontOverride = runescapeFont,
                FontColorOverride = Color.White
            };

            strokeSlider.OnValueChanged += _ => UpdatePreview();
            shadowXSlider.OnValueChanged += _ => UpdatePreview();
            shadowYSlider.OnValueChanged += _ => UpdatePreview();
            lineCapButton.OnItemSelected += args =>
            {
                lineCapButton.SelectId(args.Id);
                UpdatePreview();
            };

            lineJoinButton.OnItemSelected += args =>
            {
                lineJoinButton.SelectId(args.Id);
                UpdatePreview();
            };

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
                        Text = "Stroke Line Cap",
                        FontOverride = runescapeFont,
                        FontColorOverride = Color.White
                    },
                    lineCapButton,
                    new Label
                    {
                        Text = "Stroke Line Join",
                        FontOverride = runescapeFont,
                        FontColorOverride = Color.White
                    },
                    lineJoinButton,
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
                var strokeThickness = strokeSlider.Value;
                var strokeThicknessMarkup = strokeThickness.ToString("0.##", CultureInfo.InvariantCulture);
                var shadowX = (int) shadowXSlider.Value;
                var shadowY = (int) shadowYSlider.Value;
                var lineCapMarkup = lineCapButton.SelectedId switch
                {
                    (int) FontStrokeLineCap.Butt => "butt",
                    (int) FontStrokeLineCap.Square => "square",
                    _ => "round"
                };
                var lineJoinMarkup = lineJoinButton.SelectedId switch
                {
                    (int) FontStrokeLineJoin.Bevel => "bevel",
                    (int) FontStrokeLineJoin.Miter => "miter",
                    _ => "round"
                };

                plain.SetMessage(FormattedMessage.FromMarkupOrThrow($"[color=#f5f5f5]Plain: {previewText}[/color]"));
                shadow.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#f6f6f6][dropshadow=#000000D9 x={shadowX} y={shadowY}]Shadow: {previewText}[/dropshadow][/color]"));
                stroke.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#fff6d6][stroke=#1a1a1a thickness=\"{strokeThicknessMarkup}\" linecap=\"{lineCapMarkup}\" linejoin=\"{lineJoinMarkup}\"]Stroke: {previewText}[/stroke][/color]"));
                combo.SetMessage(
                    FormattedMessage.FromMarkupOrThrow(
                        $"[color=#f9fff0][stroke=#182538 thickness=\"{strokeThicknessMarkup}\" linecap=\"{lineCapMarkup}\" linejoin=\"{lineJoinMarkup}\"][dropshadow=#000000D9 x={shadowX} y={shadowY}]Stroke + Shadow: {previewText}[/dropshadow][/stroke][/color]"));
                sliderInfo.Text = $"Interactive settings: stroke={strokeThickness:0.##}px cap={lineCapMarkup} join={lineJoinMarkup} shadow=({shadowX}, {shadowY})";
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
