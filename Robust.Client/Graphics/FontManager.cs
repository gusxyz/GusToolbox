using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.Utility;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Client.Graphics
{
    internal sealed class FontManager : IFontManagerInternal
    {
        private const int SheetWidth = 256;
        private const int SheetHeight = 256;

        private readonly IClyde _clyde;
        private readonly ISawmill _sawmill;

        private uint _baseFontDpi = 96;

        private readonly Library _library;

        private readonly Dictionary<(FontFaceHandle, int fontSize), FontInstanceHandle> _loadedInstances =
            new();

        public FontManager(IClyde clyde, ILogManager logManager)
        {
            _clyde = clyde;
            _library = new Library();
            _sawmill = logManager.GetSawmill("font");
        }

        public IFontFaceHandle Load(Stream stream, int index = 0)
        {
            // Freetype directly operates on the font memory managed by us.
            // As such, the font data should be pinned in POH.
            var fontData = stream.CopyToPinnedArray();
            return Load(new ArrayMemoryHandle(fontData), index);
        }

        public IFontFaceHandle Load(IFontMemoryHandle memory, int index = 0)
        {
            var face = FaceLoad(memory, index);
            var handle = new FontFaceHandle(face, memory);
            return handle;
        }

        public IFontFaceHandle LoadWithPostscriptName(IFontMemoryHandle memory, string postscriptName)
        {
            var numFaces = 1;

            for (var i = 0; i < numFaces; i++)
            {
                var face = FaceLoad(memory, i);
                numFaces = face.FaceCount;

                if (face.GetPostscriptName() == postscriptName)
                    return new FontFaceHandle(face, memory);

                face.Dispose();
            }

            // Fallback, load SOMETHING.
            _sawmill.Warning($"Failed to load correct font via postscript name! {postscriptName}");
            return new FontFaceHandle(FaceLoad(memory, 0), memory);
        }

        private unsafe Face FaceLoad(IFontMemoryHandle memory, int index)
        {
            return new Face(_library,
                (nint)memory.GetData(),
                checked((int)memory.GetDataSize()),
                index);
        }

        void IFontManagerInternal.SetFontDpi(uint fontDpi)
        {
            _baseFontDpi = fontDpi;
        }

        public IFontInstanceHandle MakeInstance(IFontFaceHandle handle, int size)
        {
            var fontFaceHandle = (FontFaceHandle) handle;
            if (_loadedInstances.TryGetValue((fontFaceHandle, size), out var instance))
            {
                return instance;
            }

            instance = new FontInstanceHandle(this, size, fontFaceHandle);

            _loadedInstances.Add((fontFaceHandle, size), instance);
            return instance;
        }

        public void ClearFontCache()
        {
            foreach (var fontInstance in _loadedInstances)
            {
                fontInstance.Value.ClearSizeData();
            }
        }

        private ScaledFontData _generateScaledDatum(FontInstanceHandle instance, float scale)
        {
            var ftFace = instance.FaceHandle.Face;
            ftFace.SetCharSize(0, instance.Size, 0, (uint) (_baseFontDpi * scale));

            var ascent = ftFace.Size.Metrics.Ascender.ToInt32();
            var descent = -ftFace.Size.Metrics.Descender.ToInt32();
            var lineHeight = ftFace.Size.Metrics.Height.ToInt32();

            var data = new ScaledFontData(ascent, descent, ascent + descent, lineHeight);


            return data;
        }

        private GlyphInfo EnsureGlyphCached(
            FontInstanceHandle instance,
            ScaledFontData scaled,
            float scale,
            uint glyph,
            int strokeThickness = 0)
        {
            var stroke = Math.Max(strokeThickness, 0);
            var cacheKey = (glyph, stroke);
            // Check if already cached.
            if (scaled.GlyphInfos.TryGetValue(cacheKey, out var info))
                return info;
            info = new GlyphInfo();

            var face = instance.FaceHandle.Face;
            face.SetCharSize(0, instance.Size, 0, (uint) (_baseFontDpi * scale));
            if (stroke <= 0)
            {
                face.LoadGlyph(glyph, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                var glyphMetrics = face.Glyph.Metrics;
                info.Metrics = new CharMetrics(glyphMetrics.HorizontalBearingX.ToInt32(),
                    glyphMetrics.HorizontalBearingY.ToInt32(),
                    glyphMetrics.HorizontalAdvance.ToInt32(),
                    glyphMetrics.Width.ToInt32(),
                    glyphMetrics.Height.ToInt32());

                using var bitmap = face.Glyph.Bitmap;
                CacheBitmap(bitmap);
            }
            else
            {
                face.LoadGlyph(glyph, LoadFlags.Default | LoadFlags.NoBitmap, LoadTarget.Normal);

                var glyphMetrics = face.Glyph.Metrics;
                var advance = glyphMetrics.HorizontalAdvance.ToInt32();

                using var sourceGlyph = face.Glyph.GetGlyph();
                using var stroker = new Stroker(_library);
                stroker.Set(stroke * 64, StrokerLineCap.Round, StrokerLineJoin.Round, Fixed16Dot16.FromInt32(0));

                using var strokedGlyph = sourceGlyph.StrokeBorder(stroker, false, false);
                strokedGlyph.ToBitmap(RenderMode.Normal, new FTVector26Dot6(0, 0), true);
                var bitmapGlyph = strokedGlyph.ToBitmapGlyph();

                using var bitmap = bitmapGlyph.Bitmap;
                info.Metrics = new CharMetrics(
                    bitmapGlyph.Left,
                    bitmapGlyph.Top,
                    advance,
                    bitmap.Width,
                    bitmap.Rows);
                CacheBitmap(bitmap);
            }

            scaled.GlyphInfos.Add(cacheKey, info);
            return info;

            void CacheBitmap(FTBitmap bitmap)
            {
                if (bitmap.Pitch < 0)
                {
                    throw new NotImplementedException();
                }

                if (bitmap.Pitch == 0)
                    return;

                using var img = GlyphBitmapToImage(bitmap);
                var sheet = scaled.AtlasTextures.Count == 0 ? GenSheet() : scaled.AtlasTextures[^1];
                var (sheetW, sheetH) = sheet.Size;

                if (sheetW - scaled.CurSheetX < img.Width)
                {
                    scaled.CurSheetX = 0;
                    // +1 Adds a pixel of vertical padding, to avoid arifacts when aliasing
                    scaled.CurSheetY = scaled.CurSheetMaxY + 1;
                }

                if (sheetH - scaled.CurSheetY < img.Height)
                {
                    // Make new sheet.
                    scaled.CurSheetY = 0;
                    scaled.CurSheetX = 0;
                    scaled.CurSheetMaxY = 0;

                    sheet = GenSheet();
                }

                sheet.SetSubImage((scaled.CurSheetX, scaled.CurSheetY), img);

                info.Texture = new AtlasTexture(
                    sheet,
                    UIBox2.FromDimensions(
                        scaled.CurSheetX,
                        scaled.CurSheetY,
                        img.Width,
                        img.Height));

                scaled.CurSheetMaxY = Math.Max(scaled.CurSheetMaxY, scaled.CurSheetY + img.Height);
                // +1 adds a pixel of horizontal padding, to avoid artifacts when aliasing
                scaled.CurSheetX += img.Width + 1;
            }

            OwnedTexture GenSheet()
            {
                var sheet = _clyde.CreateBlankTexture<A8>((SheetWidth, SheetHeight),
                    $"font-{face.FamilyName}-{instance.Size}-{(uint) (_baseFontDpi * scale)}-sheet{scaled.AtlasTextures.Count}");
                scaled.AtlasTextures.Add(sheet);
                return sheet;
            }
        }

        private static Image<A8> GlyphBitmapToImage(FTBitmap bitmap)
        {
            switch (bitmap.PixelMode)
            {
                case PixelMode.Mono:
                {
                    return MonoBitMapToImage(bitmap);
                }
                case PixelMode.Gray:
                {
                    ReadOnlySpan<A8> span;
                    unsafe
                    {
                        span = new ReadOnlySpan<A8>((void*) bitmap.Buffer, bitmap.Pitch * bitmap.Rows);
                    }

                    var img = new Image<A8>(bitmap.Width, bitmap.Rows);
                    span.Blit(
                        bitmap.Pitch,
                        UIBox2i.FromDimensions(0, 0, bitmap.Pitch, bitmap.Rows),
                        img,
                        (0, 0));
                    return img;
                }
                case PixelMode.Gray2:
                case PixelMode.Gray4:
                case PixelMode.Lcd:
                case PixelMode.VerticalLcd:
                case PixelMode.Bgra:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static Image<A8> MonoBitMapToImage(FTBitmap bitmap)
        {
            DebugTools.Assert(bitmap.PixelMode == PixelMode.Mono);
            DebugTools.Assert(bitmap.Pitch > 0);

            ReadOnlySpan<byte> span;
            unsafe
            {
                span = new ReadOnlySpan<byte>((void*) bitmap.Buffer, bitmap.Rows * bitmap.Pitch);
            }

            var bitmapImage = new Image<A8>(bitmap.Width, bitmap.Rows);
            for (var y = 0; y < bitmap.Rows; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var byteIndex = y * bitmap.Pitch + (x / 8);
                    var bitIndex = x % 8;

                    var bit = (span[byteIndex] & (1 << (7 - bitIndex))) != 0;
                    bitmapImage[x, y] = new A8(bit ? byte.MaxValue : byte.MinValue);
                }
            }
            return bitmapImage;
        }

        private sealed class FontFaceHandle : IFontFaceHandle
        {
            // Keep this alive to avoid it being GC'd.
            private readonly IFontMemoryHandle _memoryHandle;
            public Face Face { get; }

            public FontFaceHandle(Face face, IFontMemoryHandle memoryHandle)
            {
                _memoryHandle = memoryHandle;
                Face = face;
            }
        }

        [PublicAPI]
        private sealed class FontInstanceHandle : IFontInstanceHandle
        {
            public FontFaceHandle FaceHandle { get; }
            public int Size { get; }
            private readonly Dictionary<float, ScaledFontData> _scaledData = new();
            private readonly FontManager _fontManager;
            public readonly Dictionary<Rune, uint> GlyphMap;

            public FontInstanceHandle(FontManager fontManager, int size, FontFaceHandle faceHandle)
            {
                GlyphMap = new Dictionary<Rune, uint>();
                _fontManager = fontManager;
                Size = size;
                FaceHandle = faceHandle;
            }

            public void ClearSizeData()
            {
                foreach (var scaleData in _scaledData)
                {
                    foreach (var ownedTexture in scaleData.Value.AtlasTextures)
                    {
                        ownedTexture.Dispose();
                    }
                }
                _scaledData.Clear();
            }

            public Texture? GetCharTexture(Rune codePoint, float scale, int strokeThickness = 0)
            {
                var glyph = GetGlyph(codePoint);
                if (glyph == 0)
                    return null;

                var scaled = GetScaleDatum(scale);
                var glyphInfo = _fontManager.EnsureGlyphCached(this, scaled, scale, glyph, strokeThickness);

                return glyphInfo.Texture;
            }

            public CharMetrics? GetCharMetrics(Rune codePoint, float scale, int strokeThickness = 0)
            {
                var glyph = GetGlyph(codePoint);
                if (glyph == 0)
                {
                    return null;
                }

                var scaled = GetScaleDatum(scale);
                var info = _fontManager.EnsureGlyphCached(this, scaled, scale, glyph, strokeThickness);

                return info.Metrics;
            }

            public int GetAscent(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Ascent;
            }

            public int GetDescent(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Descent;
            }

            public int GetHeight(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.Height;
            }

            public int GetLineHeight(float scale)
            {
                var scaled = GetScaleDatum(scale);
                return scaled.LineHeight;
            }

            private uint GetGlyph(Rune chr)
            {
                if (GlyphMap.TryGetValue(chr, out var glyph))
                {
                    return glyph;
                }

                // Check FreeType to see if it exists.
                var index = FaceHandle.Face.GetCharIndex((uint) chr.Value);

                GlyphMap.Add(chr, index);

                return index;
            }

            private ScaledFontData GetScaleDatum(float scale)
            {
                if (_scaledData.TryGetValue(scale, out var datum))
                {
                    return datum;
                }

                datum = _fontManager._generateScaledDatum(this, scale);
                _scaledData.Add(scale, datum);
                return datum;
            }
        }

        private sealed class ScaledFontData
        {
            public ScaledFontData(int ascent, int descent, int height, int lineHeight)
            {
                Ascent = ascent;
                Descent = descent;
                Height = height;
                LineHeight = lineHeight;
            }

            public readonly List<OwnedTexture> AtlasTextures = new();
            public readonly Dictionary<(uint Glyph, int StrokeThickness), GlyphInfo> GlyphInfos = new();
            public readonly int Ascent;
            public readonly int Descent;
            public readonly int Height;
            public readonly int LineHeight;

            public int CurSheetX;
            public int CurSheetY;
            public int CurSheetMaxY;
        }

        public sealed class GlyphInfo
        {
            public CharMetrics Metrics;
            public AtlasTexture? Texture;
        }

        private sealed class ArrayMemoryHandle(byte[] array) : IFontMemoryHandle
        {
            private GCHandle _gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            public unsafe byte* GetData()
            {
                return (byte*) _gcHandle.AddrOfPinnedObject();
            }

            public IntPtr GetDataSize()
            {
                return array.Length;
            }

            public void Dispose()
            {
                _gcHandle.Free();
                _gcHandle = default;
                GC.SuppressFinalize(this);
            }

            ~ArrayMemoryHandle()
            {
                Dispose();
            }
        }
    }
}
