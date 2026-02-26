using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Used by <see cref="OutputPanel"/> and <see cref="RichTextLabel"/> to handle rich text layout.
    ///     Note that if this text is ever removed or modified without removing the owning control,
    ///     then <see cref="RemoveControls"/> should be called to ensure that any controls that were added by this
    ///     entry are also removed.
    /// </summary>
    internal struct RichTextEntry
    {
        public static readonly Type[] DefaultTags =
        [
            typeof(AlignTag),
            typeof(BoldItalicTag),
            typeof(BoldTag),
            typeof(BulletTag),
            typeof(ColorTag),
            typeof(DropShadowTag),
            typeof(HeadingTag),
            typeof(ItalicTag),
            typeof(StrokeTag),
            typeof(TrackingTag)
        ];

        private readonly Color _defaultColor;
        private readonly Type[]? _tagsAllowed;

        public readonly FormattedMessage Message;

        /// <summary>
        ///     The vertical size of this entry, in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        ///     The horizontal size of this entry, in pixels.
        /// </summary>
        public int Width;

        /// <summary>
        ///     The combined text indices in the message's text tags to put line breaks.
        /// </summary>
        public ValueList<int> LineBreaks;
        public ValueList<int> LineWidths;
        public ValueList<Label.AlignMode> LineAlignments;

        public readonly Dictionary<int, Control>? Controls;


        public RichTextEntry(
            FormattedMessage message,
            Control parent,
            MarkupTagManager tagManager,
            Color? defaultColor = null) : this(message, parent, tagManager, DefaultTags, defaultColor)
        {
            // RichTextEntry constructor but with DefaultTags
        }

        public RichTextEntry(FormattedMessage message, Control parent, MarkupTagManager tagManager, Type[]? tagsAllowed, Color? defaultColor = null)
        {
            Message = message;
            Height = 0;
            Width = 0;
            LineBreaks = default;
            LineWidths = default;
            LineAlignments = default;
            _defaultColor = defaultColor ?? new(200, 200, 200);
            _tagsAllowed = tagsAllowed;
            Controls = GetControls(parent, tagManager);
        }

        private readonly Dictionary<int, Control>? GetControls(Control parent, MarkupTagManager tagManager)
        {
            Dictionary<int, Control>? tagControls = null;
            var nodeIndex = -1;

            foreach (var node in Message)
            {
                nodeIndex++;

                if (node.Name == null)
                    continue;

                if (!tagManager.TryGetMarkupTagHandler(node.Name, _tagsAllowed, out var handler) || !handler.TryCreateControl(node, out var control))
                    continue;

                // Markup tag handler instances are shared across controls. We need to ensure that the hanlder doesn't
                // store state information and return the same control for each rich text entry.
                DebugTools.Assert(handler.TryCreateControl(node, out var other) && other != control);

                parent.Children.Add(control);
                tagControls ??= new Dictionary<int, Control>();
                tagControls.Add(nodeIndex, control);
            }

            return tagControls;
        }

        // TODO RICH TEXT
        // Somehow ensure that this **has** to be called when removing rich text from some control.
        /// <summary>
        /// Remove all owned controls from their parents.
        /// </summary>
        public readonly void RemoveControls()
        {
            if (Controls == null)
                return;

            foreach (var ctrl in Controls.Values)
            {
                ctrl.Orphan();
            }
        }

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="defaultFont">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        /// <param name="lineHeightScale"></param>
        public RichTextEntry Update(MarkupTagManager tagManager, Font defaultFont, float maxSizeX, float uiScale, float lineHeightScale = 1)
        {
            // This method is gonna suck due to complexity.
            // Bear with me here.
            // I am so deeply sorry for the person adding stuff to this in the future.

            Height = defaultFont.GetHeight(uiScale);
            LineBreaks.Clear();
            LineWidths.Clear();
            LineAlignments.Clear();

            int? breakLine;
            var wordWrap = new WordWrap(maxSizeX);
            var context = new MarkupDrawingContext();
            context.Font.Push(defaultFont);
            context.Color.Push(_defaultColor);
            context.Tracking.Push(0f);

            // Go over every node.
            // Nodes can change the markup drawing context and return additional text.
            // It's also possible for nodes to return inline controls. They get treated as one large rune.
            var nodeIndex = -1;
            foreach (var node in Message)
            {
                nodeIndex++;
                var text = ProcessNode(tagManager, node, context);

                if (!context.Font.TryPeek(out var font))
                    font = defaultFont;

                // And go over every character.
                foreach (var rune in text.EnumerateRunes())
                {
                    if (ProcessRune(ref this, rune, out breakLine))
                        continue;

                    // Uh just skip unknown characters I guess.
                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
                        continue;

                    var tracking = CurrentTrackingPx(context);
                    if (tracking != 0)
                        metrics = new CharMetrics(metrics.BearingX, metrics.BearingY, metrics.Advance + tracking, metrics.Width, metrics.Height);

                    if (ProcessMetric(ref this, metrics, out breakLine))
                    {
                        UpdateLineLayoutData(tagManager, defaultFont, maxSizeX, uiScale);
                        return this;
                    }
                }

                if (Controls == null || !Controls.TryGetValue(nodeIndex, out var control))
                    continue;

                control.Measure(new Vector2(maxSizeX, Height));

                var desiredSize = control.DesiredPixelSize;
                var controlMetrics = new CharMetrics(
                    0, 0,
                    desiredSize.X,
                    desiredSize.X,
                    desiredSize.Y);

                if (ProcessMetric(ref this, controlMetrics, out breakLine))
                {
                    UpdateLineLayoutData(tagManager, defaultFont, maxSizeX, uiScale);
                    return this;
                }
            }

            Width = wordWrap.FinalizeText(out breakLine);
            CheckLineBreak(ref this, breakLine);
            UpdateLineLayoutData(tagManager, defaultFont, maxSizeX, uiScale);

            return this;

            bool ProcessRune(ref RichTextEntry src, Rune rune, out int? outBreakLine)
            {
                wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
                CheckLineBreak(ref src, breakLine);
                CheckLineBreak(ref src, breakNewLine);
                outBreakLine = breakLine;
                return skip;
            }

            bool ProcessMetric(ref RichTextEntry src, CharMetrics metrics, out int? outBreakLine)
            {
                wordWrap.NextMetrics(metrics, out breakLine, out var abort);
                CheckLineBreak(ref src, breakLine);
                outBreakLine = breakLine;
                return abort;
            }

            void CheckLineBreak(ref RichTextEntry src, int? line)
            {
                if (line is { } l)
                {
                    src.LineBreaks.Add(l);
                    if (!context.Font.TryPeek(out var font))
                        font = defaultFont;

                    src.Height += GetLineHeight(font, uiScale, lineHeightScale);
                }
            }
        }

        internal readonly void HideControls()
        {
            if (Controls == null)
                return;

            foreach (var control in Controls.Values)
            {
                control.Visible = false;
            }
        }

        public readonly void Draw(
            MarkupTagManager tagManager,
            DrawingHandleBase handle,
            Font defaultFont,
            UIBox2 drawBox,
            float verticalOffset,
            MarkupDrawingContext context,
            float uiScale,
            float lineHeightScale = 1,
            bool drawBoxIsPixelSpace = true)
        {
            context.Clear();
            context.Color.Push(_defaultColor);
            context.Font.Push(defaultFont);
            context.Align.Push(Label.AlignMode.Left);
            context.Tracking.Push(0f);

            var lineWidths = LineWidths;
            var lineAlignments = LineAlignments;
            var drawScale = drawBoxIsPixelSpace ? 1f : uiScale;
            var drawLeft = drawBox.Left * drawScale;
            var drawTop = drawBox.Top * drawScale;
            var drawWidth = drawBox.Width * drawScale;
            var drawVerticalOffset = verticalOffset * drawScale;

            var globalBreakCounter = 0;
            var lineBreakIndex = 0;
            var currentLine = 0;
            var baseLine = new Vector2(
                drawLeft + GetLineOffset(currentLine),
                drawTop + defaultFont.GetAscent(uiScale) + drawVerticalOffset);
            var controlYAdvance = 0f;

            var spaceRune = new Rune(' ');

            var nodeIndex = -1;
            foreach (var node in Message)
            {
                nodeIndex++;
                var text = ProcessNode(tagManager, node, context);
                if (!context.Color.TryPeek(out var color) || !context.Font.TryPeek(out var font))
                {
                    color = _defaultColor;
                    font = defaultFont;
                }

                foreach (var rune in text.EnumerateRunes())
                {
                    var skipSpaceBaseline = false;
                    if (lineBreakIndex < LineBreaks.Count &&
                        LineBreaks[lineBreakIndex] == globalBreakCounter)
                    {
                        currentLine += 1;
                        baseLine = new Vector2(
                            drawLeft + GetLineOffset(currentLine),
                            baseLine.Y + GetLineHeight(font, uiScale, lineHeightScale) + controlYAdvance);
                        controlYAdvance = 0;
                        lineBreakIndex += 1;

                        // The baseline calc is kind of messed up, the newline is After the space but the space is being drawn after doing the newline
                        // Which means if this metric Ends on a space, the next metric will use the wrong baseline when it starts, for some reason ..
                        if (rune == spaceRune)
                            skipSpaceBaseline = true;
                    }

                    if (!Rune.IsWhiteSpace(rune))
                    {
                        tagManager.DrawBeforeGlyph(handle, font, rune, baseLine, uiScale, context);
                    }

                    var advance = font.DrawChar(handle, rune, baseLine, uiScale, color);
                    var tracking = CurrentTracking(context);

                    if (!skipSpaceBaseline)
                        baseLine += new Vector2(advance + tracking, 0);

                    globalBreakCounter += 1;
                }

                if (Controls == null || !Controls.TryGetValue(nodeIndex, out var control))
                    continue;

                // Controls may have been previously hidden via HideControls due to being "out-of frame".
                // If this ever gets replaced with RectClipContents / scissor box testing, this can be removed.
                control.Visible = true;

                var invertedScale = 1f / uiScale;
                control.Measure(new Vector2(Width, Height));
                control.Arrange(UIBox2.FromDimensions(
                    baseLine.X * invertedScale,
                    (baseLine.Y - defaultFont.GetAscent(uiScale)) * invertedScale,
                    control.DesiredSize.X,
                    control.DesiredSize.Y
                ));
                var advanceX = control.DesiredPixelSize.X;
                controlYAdvance = Math.Max(0f, (control.DesiredPixelSize.Y - GetLineHeight(font, uiScale, lineHeightScale)) * invertedScale);
                baseLine += new Vector2(advanceX, 0);
            }

            float GetLineOffset(int line)
            {
                var lineWidth = line < lineWidths.Count ? lineWidths[line] : 0;
                var lineAlign = line < lineAlignments.Count ? lineAlignments[line] : Label.AlignMode.Left;

                return lineAlign switch
                {
                    Label.AlignMode.Left => 0f,
                    Label.AlignMode.Center or Label.AlignMode.Fill => MathF.Max((drawWidth - lineWidth) / 2f, 0f),
                    Label.AlignMode.Right => MathF.Max(drawWidth - lineWidth, 0f),
                    _ => throw new ArgumentOutOfRangeException(nameof(lineAlign), lineAlign, null)
                };
            }
        }

        private void UpdateLineLayoutData(MarkupTagManager tagManager, Font defaultFont, float maxSizeX, float uiScale)
        {
            LineWidths.Clear();
            LineAlignments.Clear();
            LineWidths.Add(0);
            LineAlignments.Add(Label.AlignMode.Left);

            var context = new MarkupDrawingContext();
            context.Color.Push(_defaultColor);
            context.Font.Push(defaultFont);
            context.Align.Push(Label.AlignMode.Left);
            context.Tracking.Push(0f);

            var globalBreakCounter = 0;
            var lineBreakIndex = 0;
            var lineIndex = 0;
            var lineWidth = 0f;
            var lineStarted = false;
            var spaceRune = new Rune(' ');

            var nodeIndex = -1;
            foreach (var node in Message)
            {
                nodeIndex++;
                var text = ProcessNode(tagManager, node, context);
                if (!context.Font.TryPeek(out var font))
                    font = defaultFont;

                foreach (var rune in text.EnumerateRunes())
                {
                    var skipWrappedSpace = false;
                    if (lineBreakIndex < LineBreaks.Count &&
                        LineBreaks[lineBreakIndex] == globalBreakCounter)
                    {
                        LineWidths[lineIndex] = (int) MathF.Ceiling(lineWidth);
                        lineIndex += 1;
                        if (lineIndex >= LineWidths.Count)
                        {
                            LineWidths.Add(0);
                            LineAlignments.Add(CurrentAlign(context));
                        }

                        lineWidth = 0f;
                        lineStarted = false;
                        lineBreakIndex += 1;

                        if (rune == spaceRune)
                            skipWrappedSpace = true;
                    }

                    if (skipWrappedSpace)
                    {
                        globalBreakCounter += 1;
                        continue;
                    }

                    if (!lineStarted)
                    {
                        LineAlignments[lineIndex] = CurrentAlign(context);
                        lineStarted = true;
                    }

                    if (font.TryGetCharMetrics(rune, uiScale, out var metrics))
                        lineWidth += metrics.Advance + CurrentTracking(context);

                    globalBreakCounter += 1;
                }

                if (Controls == null || !Controls.TryGetValue(nodeIndex, out var control))
                    continue;

                if (!lineStarted)
                {
                    LineAlignments[lineIndex] = CurrentAlign(context);
                    lineStarted = true;
                }

                control.Measure(new Vector2(maxSizeX, Height));
                lineWidth += control.DesiredPixelSize.X;
            }

            LineWidths[lineIndex] = (int) MathF.Ceiling(lineWidth);
            return;

            static Label.AlignMode CurrentAlign(MarkupDrawingContext context)
            {
                return context.Align.TryPeek(out var align)
                    ? align
                    : Label.AlignMode.Left;
            }

            static float CurrentTracking(MarkupDrawingContext context)
            {
                return context.Tracking.TryPeek(out var tracking)
                    ? tracking
                    : 0f;
            }
        }

        private readonly string ProcessNode(MarkupTagManager tagManager, MarkupNode node, MarkupDrawingContext context)
        {
            // If a nodes name is null it's a text node.
            if (node.Name == null)
                return node.Value.StringValue ?? "";

            //Skip the node if there is no markup tag for it.
            if (!tagManager.TryGetMarkupTagHandler(node.Name, _tagsAllowed, out var tag))
                return "";

            if (!node.Closing)
            {
                tag.PushDrawContext(node, context);
                return tag.TextBefore(node);
            }

            tag.PopDrawContext(node, context);
            return tag.TextAfter(node);
        }

        private static int GetLineHeight(Font font, float uiScale, float lineHeightScale)
        {
            var height = font.GetLineHeight(uiScale);
            return (int)(height * lineHeightScale);
        }

        private static float CurrentTracking(MarkupDrawingContext context)
        {
            return context.Tracking.TryPeek(out var tracking)
                ? tracking
                : 0f;
        }

        private static int CurrentTrackingPx(MarkupDrawingContext context)
        {
            return (int) MathF.Round(CurrentTracking(context));
        }
    }
}
