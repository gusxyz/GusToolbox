using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabFontShadow : Control
    {
        public TabFontShadow()
        {
            TabContainer.SetTabTitle(this, nameof(Tab.FontShadow));

            var heading = new Label
            {
                Text = "Font Stroke + Drop Shadow Preview",
                FontColorOverride = Color.White,
                FontColorShadowOverride = Color.Black.WithAlpha(0.8f),
                ShadowOffsetXOverride = 1,
                ShadowOffsetYOverride = 1,
                FontColorStrokeOverride = Color.FromHex("#161616"),
                StrokeThicknessOverride = 1,
            };

            var plain = new Label
            {
                Text = "Plain: The quick brown fox 0123456789",
                FontColorOverride = Color.FromHex("#f5f5f5"),
            };

            var shadow = new Label
            {
                Text = "Shadow: The quick brown fox 0123456789",
                FontColorOverride = Color.FromHex("#f6f6f6"),
                FontColorShadowOverride = Color.Black.WithAlpha(0.85f),
                ShadowOffsetXOverride = 2,
                ShadowOffsetYOverride = 2,
            };

            var stroke = new Label
            {
                Text = "Stroke: The quick brown fox 0123456789",
                FontColorOverride = Color.FromHex("#fff6d6"),
                FontColorStrokeOverride = Color.FromHex("#1a1a1a"),
                StrokeThicknessOverride = 2,
            };

            var combo = new Label
            {
                Text = "Stroke + Shadow: The quick brown fox 0123456789",
                FontColorOverride = Color.FromHex("#f9fff0"),
                FontColorStrokeOverride = Color.FromHex("#182538"),
                StrokeThicknessOverride = 2,
                FontColorShadowOverride = Color.Black.WithAlpha(0.85f),
                ShadowOffsetXOverride = 2,
                ShadowOffsetYOverride = 2,
            };

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
                        FontColorOverride = Color.White
                    },
                    strokeSlider,
                    new Label
                    {
                        Text = "Shadow Offset X",
                        FontColorOverride = Color.White
                    },
                    shadowXSlider,
                    new Label
                    {
                        Text = "Shadow Offset Y",
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

                stroke.StrokeThicknessOverride = strokeThickness;

                combo.StrokeThicknessOverride = strokeThickness;
                combo.ShadowOffsetXOverride = shadowX;
                combo.ShadowOffsetYOverride = shadowY;

                shadow.ShadowOffsetXOverride = shadowX;
                shadow.ShadowOffsetYOverride = shadowY;

                sliderInfo.Text = $"Interactive settings: stroke={strokeThickness}px shadow=({shadowX}, {shadowY})";
            }
        }
    }
}
