using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace geometry.materials.image
{
    // Uses logic from the excellent (LGPL-licensed) VtfLib, courtesy of Neil Jedrzejewski & Ryan Gregg
    public class ImageFormatInfo
    {
        private static readonly Dictionary<ImageFormat, ImageFormatInfo?> imageFormats =
            new Dictionary<ImageFormat, ImageFormatInfo?>
            {
                {ImageFormat.None, null},
                {
                    ImageFormat.RGBA8,
                    new ImageFormatInfo(ImageFormat.RGBA8, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)
                },
                {
                    ImageFormat.ABGR8,
                    new ImageFormatInfo(ImageFormat.ABGR8, 32, 4, 8, 8, 8, 8, 3, 2, 1, 0, false, true)
                },
                {
                    ImageFormat.RGB8,
                    new ImageFormatInfo(ImageFormat.RGB8, 24, 3, 8, 8, 8, 0, 0, 1, 2, -1, false, true)
                },
                {
                    ImageFormat.BGR8,
                    new ImageFormatInfo(ImageFormat.BGR8, 24, 3, 8, 8, 8, 0, 2, 1, 0, -1, false, true)
                },
                {
                    ImageFormat.RGB565,
                    new ImageFormatInfo(ImageFormat.RGB565, 16, 2, 5, 6, 5, 0, 0, 1, 2, -1, false, true)
                },
                {
                    ImageFormat.I8,
                    new ImageFormatInfo(ImageFormat.I8, 8, 1, 8, 8, 8, 0, 0, -1, -1, -1, false, true,
                        TransformLuminance)
                },
                {
                    ImageFormat.IA8,
                    new ImageFormatInfo(ImageFormat.IA8, 16, 2, 8, 8, 8, 8, 0, -1, -1, 1, false, true,
                        TransformLuminance)
                },
                {ImageFormat.P8, new ImageFormatInfo(ImageFormat.P8, 8, 1, 0, 0, 0, 0, -1, -1, -1, -1, false, false)},
                {ImageFormat.A8, new ImageFormatInfo(ImageFormat.A8, 8, 1, 0, 0, 0, 8, -1, -1, -1, 0, false, true)},
                {
                    ImageFormat.RGB8Bluescreen,
                    new ImageFormatInfo(ImageFormat.RGB8Bluescreen, 24, 3, 8, 8, 8, 8, 0, 1, 2, -1, false, true,
                        TransformBluescreen)
                },
                {
                    ImageFormat.BGR8Bluescreen,
                    new ImageFormatInfo(ImageFormat.BGR8Bluescreen, 24, 3, 8, 8, 8, 8, 2, 1, 0, -1, false, true,
                        TransformBluescreen)
                },
                {
                    ImageFormat.ARGB8,
                    new ImageFormatInfo(ImageFormat.ARGB8, 32, 4, 8, 8, 8, 8, 3, 0, 1, 2, false, true)
                },
                {
                    ImageFormat.BGRA8,
                    new ImageFormatInfo(ImageFormat.BGRA8, 32, 4, 8, 8, 8, 8, 2, 1, 0, 3, false, true)
                },
                {ImageFormat.DXT1, new ImageFormatInfo(ImageFormat.DXT1, 4, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, true)},
                {ImageFormat.DXT3, new ImageFormatInfo(ImageFormat.DXT3, 8, 0, 0, 0, 0, 8, -1, -1, -1, -1, true, true)},
                {ImageFormat.DXT5, new ImageFormatInfo(ImageFormat.DXT5, 8, 0, 0, 0, 0, 8, -1, -1, -1, -1, true, true)},
                {
                    ImageFormat.BGRX8,
                    new ImageFormatInfo(ImageFormat.BGRX8, 32, 4, 8, 8, 8, 0, 2, 1, 0, -1, false, true)
                },
                {
                    ImageFormat.BGR565,
                    new ImageFormatInfo(ImageFormat.BGR565, 16, 2, 5, 6, 5, 0, 2, 1, 0, -1, false, true)
                },
                {
                    ImageFormat.BGRX5551,
                    new ImageFormatInfo(ImageFormat.BGRX5551, 16, 2, 5, 5, 5, 0, 2, 1, 0, -1, false, true)
                },
                {
                    ImageFormat.BGRA4,
                    new ImageFormatInfo(ImageFormat.BGRA4, 16, 2, 4, 4, 4, 4, 2, 1, 0, 3, false, true)
                },
                {
                    ImageFormat.DXT1OneBitAlpha,
                    new ImageFormatInfo(ImageFormat.DXT1OneBitAlpha, 4, 0, 0, 0, 0, 1, -1, -1, -1, -1, true, true)
                },
                {
                    ImageFormat.BGRA5551,
                    new ImageFormatInfo(ImageFormat.BGRA5551, 16, 2, 5, 5, 5, 1, 2, 1, 0, 3, false, true)
                },
                {ImageFormat.UV8, new ImageFormatInfo(ImageFormat.UV8, 16, 2, 8, 8, 0, 0, 0, 1, -1, -1, false, true)},
                {
                    ImageFormat.UVWQ8,
                    new ImageFormatInfo(ImageFormat.UVWQ8, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)
                },
                {
                    ImageFormat.RGBA16F,
                    new ImageFormatInfo(ImageFormat.RGBA16F, 64, 8, 16, 16, 16, 16, 0, 1, 2, 3, false, true)
                },
                {
                    ImageFormat.RGBA16,
                    new ImageFormatInfo(ImageFormat.RGBA16, 64, 8, 16, 16, 16, 16, 0, 1, 2, 3, false, true)
                },
                {
                    ImageFormat.UVLX8,
                    new ImageFormatInfo(ImageFormat.UVLX8, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)
                },
                {
                    ImageFormat.R32F,
                    new ImageFormatInfo(ImageFormat.R32F, 32, 4, 32, 0, 0, 0, 0, -1, -1, -1, false, false)
                },
                {
                    ImageFormat.RGB32F,
                    new ImageFormatInfo(ImageFormat.RGB32F, 96, 12, 32, 32, 32, 0, 0, 1, 2, -1, false, false)
                },
                {
                    ImageFormat.RGBA32F,
                    new ImageFormatInfo(ImageFormat.RGBA32F, 128, 16, 32, 32, 32, 32, 0, 1, 2, 3, false, false)
                },
                {
                    ImageFormat.NvDst16,
                    new ImageFormatInfo(ImageFormat.NvDst16, 16, 2, 16, 0, 0, 0, 0, -1, -1, -1, false, true)
                },
                {
                    ImageFormat.NvDst24,
                    new ImageFormatInfo(ImageFormat.NvDst24, 24, 3, 24, 0, 0, 0, 0, -1, -1, -1, false, true)
                },
                {
                    ImageFormat.NvIntZ,
                    new ImageFormatInfo(ImageFormat.NvIntZ, 32, 4, 0, 0, 0, 0, -1, -1, -1, -1, false, false)
                },
                {
                    ImageFormat.NvRawZ,
                    new ImageFormatInfo(ImageFormat.NvRawZ, 24, 3, 0, 0, 0, 0, -1, -1, -1, -1, false, false)
                },
                {
                    ImageFormat.AtiDst16,
                    new ImageFormatInfo(ImageFormat.AtiDst16, 16, 2, 16, 0, 0, 0, 0, -1, -1, -1, false, true)
                },
                {
                    ImageFormat.AtiDst24,
                    new ImageFormatInfo(ImageFormat.AtiDst24, 24, 3, 24, 0, 0, 0, 0, -1, -1, -1, false, true)
                },
                {
                    ImageFormat.NvNull,
                    new ImageFormatInfo(ImageFormat.NvNull, 32, 4, 0, 0, 0, 0, -1, -1, -1, -1, false, false)
                },
                {
                    ImageFormat.Ati1N,
                    new ImageFormatInfo(ImageFormat.Ati1N, 4, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, false)
                },
                {
                    ImageFormat.Ati2N,
                    new ImageFormatInfo(ImageFormat.Ati2N, 8, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, false)
                }
            };

        private readonly bool _is16Aligned;
        private readonly bool _is32Aligned;

        private readonly bool _is8Aligned;
        private readonly Mask[]? _masks;

        private readonly TransformPixel? _pixelTransform;

        private ImageFormatInfo(
            ImageFormat format,
            int bitsPerPixel, int bytesPerPixel,
            int redBitsPerPixel, int greenBitsPerPixel, int blueBitsPerPixel, int alphaBitsPerPixel,
            int redIndex, int greenIndex, int blueIndex, int alphaIndex,
            bool isCompressed, bool isSupported,
            TransformPixel? pixelTransform = null
        )
        {
            Format = format;

            BitsPerPixel = bitsPerPixel;
            BytesPerPixel = bytesPerPixel;

            RedBitsPerPixel = redBitsPerPixel;
            GreenBitsPerPixel = greenBitsPerPixel;
            BlueBitsPerPixel = blueBitsPerPixel;
            AlphaBitsPerPixel = alphaBitsPerPixel;

            RedIndex = redIndex;
            GreenIndex = greenIndex;
            BlueIndex = blueIndex;
            AlphaIndex = alphaIndex;

            IsCompressed = isCompressed;
            IsSupported = isSupported;

            _pixelTransform = pixelTransform;

            _is8Aligned = (redBitsPerPixel == 0 || redBitsPerPixel == 8) &&
                          (greenBitsPerPixel == 0 || greenBitsPerPixel == 8) &&
                          (blueBitsPerPixel == 0 || blueBitsPerPixel == 8) &&
                          (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 8);

            _is16Aligned = (redBitsPerPixel == 0 || redBitsPerPixel == 16) &&
                           (greenBitsPerPixel == 0 || greenBitsPerPixel == 16) &&
                           (blueBitsPerPixel == 0 || blueBitsPerPixel == 16) &&
                           (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 16);

            _is32Aligned = (redBitsPerPixel == 0 || redBitsPerPixel == 32) &&
                           (greenBitsPerPixel == 0 || greenBitsPerPixel == 32) &&
                           (blueBitsPerPixel == 0 || blueBitsPerPixel == 32) &&
                           (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 32);

            if (_is8Aligned || _is16Aligned || _is32Aligned)
                return;

            var masks = new[]
            {
                new Mask('r', redBitsPerPixel, redIndex),
                new Mask('g', greenBitsPerPixel, greenIndex),
                new Mask('b', blueBitsPerPixel, blueIndex),
                new Mask('a', alphaBitsPerPixel, alphaIndex)
            }.OrderBy(x => x.Index).ToList();

            var offset = bitsPerPixel;
            foreach (var m in masks)
            {
                offset -= m.Size;
                m.Offset = offset;
            }

            var dict = masks.ToDictionary(x => x.Component, x => x);
            _masks = new[] {dict['b'], dict['g'], dict['r'], dict['a']};
        }

        public ImageFormat Format { get; }

        public int BitsPerPixel { get; }
        public int BytesPerPixel { get; }

        public int RedBitsPerPixel { get; }
        public int GreenBitsPerPixel { get; }
        public int BlueBitsPerPixel { get; }
        public int AlphaBitsPerPixel { get; }

        public int RedIndex { get; }
        public int GreenIndex { get; }
        public int BlueIndex { get; }
        public int AlphaIndex { get; }

        public bool IsCompressed { get; }
        public bool IsSupported { get; }

        public static ImageFormatInfo? FromFormat(ImageFormat imageFormat)
        {
            return imageFormats[imageFormat];
        }

        /// <summary>
        ///     Gets the size of the image data for this format in bytes
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <returns>The size of the image, in bytes</returns>
        public int GetDataSize(int width, int height)
        {
            switch (Format)
            {
                case ImageFormat.DXT1:
                case ImageFormat.DXT1OneBitAlpha:
                    if (width < 4 && width > 0) width = 4;
                    if (height < 4 && height > 0) height = 4;
                    return (width + 3) / 4 * ((height + 3) / 4) * 8;
                case ImageFormat.DXT3:
                case ImageFormat.DXT5:
                    if (width < 4 && width > 0) width = 4;
                    if (height < 4 && height > 0) height = 4;
                    return (width + 3) / 4 * ((height + 3) / 4) * 16;
                default:
                    return width * height * BytesPerPixel;
            }
        }

        /// <summary>
        ///     Convert an array of data in this format to a standard bgra8888 format.
        /// </summary>
        /// <param name="data">The data in this format</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <returns>The data in bgra8888 format.</returns>
        public byte[] ConvertToBGRA32(byte[] data, int width, int height)
        {
            var buffer = new byte[width * height * 4];

            switch (Format)
            {
                // No format, return blank array
                // This is the exact format we want, take the fast path
                case ImageFormat.None:
                    return buffer;
                // Handle compressed formats
                case ImageFormat.BGRA8:
                    Array.Copy(data, buffer, buffer.Length);
                    return buffer;
                default:
                {
                    if (IsCompressed)
                    {
                        switch (Format)
                        {
                            case ImageFormat.DXT1:
                            case ImageFormat.DXT1OneBitAlpha:
                                DxtFormat.DecompressDxt1(buffer, data, width, height);
                                break;
                            case ImageFormat.DXT3:
                                DxtFormat.DecompressDxt3(buffer, data, width, height);
                                break;
                            case ImageFormat.DXT5:
                                DxtFormat.DecompressDxt5(buffer, data, width, height);
                                break;
                            default:
                                throw new NotImplementedException($"Unsupported format: {Format}");
                        }

                        return buffer;
                    }

                    // Handle simple byte-aligned data

                    if (_is8Aligned)
                    {
                        for (int i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                        {
                            buffer[j + 0] = BlueIndex >= 0 ? data[i + BlueIndex] : (byte) 0; // b
                            buffer[j + 1] = GreenIndex >= 0 ? data[i + GreenIndex] : (byte) 0; // g
                            buffer[j + 2] = RedIndex >= 0 ? data[i + RedIndex] : (byte) 0; // r
                            buffer[j + 3] = AlphaIndex >= 0 ? data[i + AlphaIndex] : (byte) 255; // a
                            _pixelTransform?.Invoke(buffer, j, 4);
                        }

                        return buffer;
                    }

                    // Special logic for half-precision HDR format

                    if (Format == ImageFormat.RGBA16F)
                    {
                        var logAverageLuminance = 0.0f;

                        var shorts = new ushort[data.Length / 2];
                        for (int i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                        {
                            for (var k = 0; k < 4; k++) shorts[j + k] = BitConverter.ToUInt16(data, i + k * 2);

                            var lum = shorts[j + 0] * 0.299f + shorts[j + 1] * 0.587f + shorts[j + 2] * 0.114f;
                            logAverageLuminance += (float) Math.Log(0.0000000001d + lum);
                        }

                        logAverageLuminance = (float) Math.Exp(logAverageLuminance / (width * height));

                        for (var i = 0; i < shorts.Length; i += 4)
                        {
                            TransformFp16(shorts, i, logAverageLuminance);

                            buffer[i + 2] = (byte) (shorts[i + 0] >> 8);
                            buffer[i + 1] = (byte) (shorts[i + 1] >> 8);
                            buffer[i + 0] = (byte) (shorts[i + 2] >> 8);
                            buffer[i + 3] = (byte) (shorts[i + 3] >> 8);
                        }

                        return buffer;
                    }

                    // Handle short-aligned data

                    if (_is16Aligned)
                    {
                        for (int i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                        {
                            var b = BlueIndex >= 0 ? BitConverter.ToUInt16(data, i + BlueIndex * 2) : ushort.MinValue;
                            var g = GreenIndex >= 0 ? BitConverter.ToUInt16(data, i + GreenIndex * 2) : ushort.MinValue;
                            var r = RedIndex >= 0 ? BitConverter.ToUInt16(data, i + RedIndex * 2) : ushort.MinValue;
                            var a = AlphaIndex >= 0 ? BitConverter.ToUInt16(data, i + AlphaIndex * 2) : ushort.MaxValue;

                            buffer[j + 0] = (byte) (b >> 8);
                            buffer[j + 1] = (byte) (g >> 8);
                            buffer[j + 2] = (byte) (r >> 8);
                            buffer[j + 3] = (byte) (a >> 8);

                            _pixelTransform?.Invoke(buffer, j, 4);
                        }

                        return buffer;
                    }

                    // Handle custom-aligned data that fits into a uint

                    if (BitsPerPixel > 32)
                        throw new NotImplementedException($"Unsupported format: {Format}");

                    Debug.Assert(_masks != null);
                    for (int i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                    {
                        var val = 0u;
                        for (var k = BytesPerPixel - 1; k >= 0; k--)
                        {
                            val <<= 8;
                            val |= data[i + k];
                        }

                        buffer[j + 0] = _masks[0].Apply(val, BitsPerPixel);
                        buffer[j + 1] = _masks[1].Apply(val, BitsPerPixel);
                        buffer[j + 2] = _masks[2].Apply(val, BitsPerPixel);
                        buffer[j + 3] = _masks[3].Apply(val, BitsPerPixel);
                    }

                    return buffer;
                }
            }
        }

        private static void TransformFp16(IList<ushort> shorts, int offset, float logAverageLuminance)
        {
            const float fp16HdrKey = 4.0f;
            const float fp16HdrShift = 0.0f;
            const float fp16HdrGamma = 2.25f;

            float sR = shorts[offset + 0], sG = shorts[offset + 1], sB = shorts[offset + 2];

            var sY = sR * 0.299f + sG * 0.587f + sB * 0.114f;

            var sU = (sB - sY) * 0.565f;
            var sV = (sR - sY) * 0.713f;

            var sTemp = sY;

            sTemp = fp16HdrKey * sTemp / logAverageLuminance;
            sTemp /= 1.0f + sTemp;
            sTemp /= sY;

            shorts[offset + 0] = Clamp(Math.Pow((sY + 1.403f * sV) * sTemp + fp16HdrShift, fp16HdrGamma) * 65535.0f);
            shorts[offset + 1] = Clamp(Math.Pow((sY - 0.344f * sU - 0.714f * sV) * sTemp + fp16HdrShift, fp16HdrGamma) *
                                       65535.0f);
            shorts[offset + 2] = Clamp(Math.Pow((sY + 1.770f * sU) * sTemp + fp16HdrShift, fp16HdrGamma) * 65535.0f);

            static ushort Clamp(double sValue)
            {
                if (sValue < ushort.MinValue) return ushort.MinValue;
                if (sValue > ushort.MaxValue) return ushort.MaxValue;
                return (ushort) sValue;
            }
        }

        private static void TransformBluescreen(byte[] bytes, int index, int count)
        {
            for (var i = index; i < index + count; i += 4)
                if (bytes[i + 0] == byte.MaxValue && bytes[i + 1] == 0 && bytes[i + 2] == 0)
                    bytes[i + 3] = 0;
        }

        private static void TransformLuminance(byte[] bytes, int index, int count)
        {
            for (var i = index; i < index + count; i += 4)
            {
                bytes[i + 0] = bytes[i + 2];
                bytes[i + 1] = bytes[i + 2];
            }
        }

        private static byte PartialToByte(byte partial, int bits)
        {
            byte b = 0;
            var dest = 8;
            while (dest >= bits)
            {
                b <<= bits;
                b |= partial;
                dest -= bits;
            }

            if (dest == 0)
                return b;

            partial >>= bits - dest;
            b <<= dest;
            b |= partial;
            return b;
        }

        private delegate void TransformPixel(byte[] data, int offset, int count);

        private class Mask
        {
            public Mask(char component, int size, int index)
            {
                Component = component;
                Size = size;
                Index = index;
            }

            public char Component { get; }
            public int Size { get; }
            public int Index { get; }
            public int Offset { get; set; }
            private uint Bitmask => ~0u >> (32 - Size);

            public byte Apply(uint value, int bitsPerPixel)
            {
                if (Index < 0) return Component == 'a' ? byte.MaxValue : byte.MinValue;
                var im = value >> (bitsPerPixel - Offset - Size);
                im &= Bitmask;
                return PartialToByte((byte) im, Size);
            }
        }
    }
}
