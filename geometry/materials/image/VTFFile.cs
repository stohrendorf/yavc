using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using geometry.components;

namespace geometry.materials.image;

public sealed class VTFFile
{
    private const string VTFHeaderId = "VTF";
    internal readonly int Height;

    internal readonly int Width;


    public VTFFile(string filename, bool onlyHeaders = false)
    {
        using var reader = new BinaryReader(File.Open(filename, FileMode.Open));

        var header = Encoding.ASCII.GetString(reader.ReadBytes(3));
        if (header != VTFHeaderId)
            throw new Exception("Invalid VTF header. Expected '" + VTFHeaderId + "', got '" + header + "'.");

        if (reader.ReadByte() != 0) throw new Exception();


        var v1 = reader.ReadUInt32();
        var v2 = reader.ReadUInt32();
        var version = v1 + v2 / 10m; // e.g. 7.3
        Header.Version = version;

        var headerSize = reader.ReadUInt32();
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();

        Header.Flags = (ImageFlags)reader.ReadUInt32();

        var numFrames = reader.ReadUInt16();
        var firstFrame = reader.ReadUInt16();

        reader.ReadBytes(4); // padding

        Header.Reflectivity = new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        reader.ReadBytes(4); // padding

        Header.BumpmapScale = reader.ReadSingle();

        var highResImageFormat = (ImageFormat)reader.ReadUInt32();
        var mipmapCount = reader.ReadByte();
        var lowResImageFormat = (ImageFormat)reader.ReadUInt32();
        var lowResWidth = reader.ReadByte();
        var lowResHeight = reader.ReadByte();

        ushort depth = 1;
        uint numResources = 0;

        if (version >= 7.2m) depth = reader.ReadUInt16();

        if (version >= 7.3m)
        {
            reader.ReadBytes(3);
            numResources = reader.ReadUInt32();
            reader.ReadBytes(8);
        }

        var faces = 1;
        if (Header.Flags.HasFlag(ImageFlags.Envmap)) faces = version < 7.5m && firstFrame != 0xFFFF ? 7 : 6;

        var highResFormatInfo = ImageFormatInfo.FromFormat(highResImageFormat);
        Debug.Assert(highResFormatInfo is not null);
        var lowResFormatInfo = ImageFormatInfo.FromFormat(lowResImageFormat);

        var thumbnailSize = lowResImageFormat == ImageFormat.None
            ? 0
            : lowResFormatInfo!.GetDataSize(lowResWidth, lowResHeight);

        var thumbnailPos = headerSize;
        var dataPos = headerSize + thumbnailSize;

        for (var i = 0; i < numResources; i++)
        {
            var type = (ResourceType)reader.ReadUInt32();
            var data = reader.ReadUInt32();
            switch (type)
            {
                case ResourceType.LowResImage:
                    // Low res image
                    thumbnailPos = data;
                    break;
                case ResourceType.Image:
                    // Regular image
                    dataPos = data;
                    break;
                case ResourceType.Sheet:
                case ResourceType.Crc:
                case ResourceType.TextureLodSettings:
                case ResourceType.TextureSettingsEx:
                case ResourceType.KeyValueData:
                    // todo
                    Resources.Add(new Resource
                    {
                        Type = type,
                        Data = data,
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), (uint)type, "Unknown resource type");
            }
        }

        if (lowResImageFormat != ImageFormat.None)
        {
            reader.BaseStream.Position = thumbnailPos;
            var thumbSize = lowResFormatInfo!.GetDataSize(lowResWidth, lowResHeight);
            LowResImage = new Image
            {
                Format = lowResImageFormat,
                Width = lowResWidth,
                Height = lowResHeight,
                Data = reader.ReadBytes(thumbSize),
            };
        }

        reader.BaseStream.Position = dataPos;
        for (var mip = mipmapCount - 1; mip >= 0; mip--)
        for (var frame = 0; frame < numFrames; frame++)
        for (var face = 0; face < faces; face++)
        for (var slice = 0; slice < depth; slice++)
        {
            var wid = GetMipSize(Width, mip);
            var hei = GetMipSize(Height, mip);
            var size = highResFormatInfo.GetDataSize(wid, hei);

            Images.Add(new Image
            {
                Format = highResImageFormat,
                Width = wid,
                Height = hei,
                Mipmap = mip,
                Frame = frame,
                Face = face,
                Slice = slice,
                Data = onlyHeaders ? null : reader.ReadBytes(size),
            });
        }
    }

    public Header Header { get; } = new();
    public List<Resource> Resources { get; } = [];
    public Image? LowResImage { get; }
    public List<Image> Images { get; } = [];

    private static int GetMipSize(int input, int level)
    {
        var res = input >> level;
        if (res < 1) res = 1;

        return res;
    }
}