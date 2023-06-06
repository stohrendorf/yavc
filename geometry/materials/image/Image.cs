namespace geometry.materials.image;

/// <summary>
///   A VTF image containing binary pixel data in some format.
/// </summary>
public sealed class Image
{
  /// <summary>
  ///   The format of this image.
  /// </summary>
  public ImageFormat Format { get; internal init; }

  public ImageFormatInfo? FormatInfo => ImageFormatInfo.FromFormat(Format);

  /// <summary>
  ///   The width of the image, in pixels
  /// </summary>
  public int Width { get; internal init; }

  /// <summary>
  ///   The height of the image, in pixels
  /// </summary>
  public int Height { get; internal init; }

  /// <summary>
  ///   The mipmap number of this image. Lower numbers = larger size.
  /// </summary>
  public int Mipmap { get; internal init; }

  /// <summary>
  ///   The frame number of this image.
  /// </summary>
  internal int Frame { get; set; }

  /// <summary>
  ///   The face number of this image.
  /// </summary>
  internal int Face { get; set; }

  /// <summary>
  ///   The slice (depth) number of this image.
  /// </summary>
  internal int Slice { get; set; }

  /// <summary>
  ///   The image data, in native image format
  /// </summary>
  internal byte[]? Data { get; init; }

  /// <summary>
  ///   Convert the native format data to a standard 32-bit bgra8888 format.
  /// </summary>
  /// <returns>The data in bgra8888 format.</returns>
  public byte[]? GetBGRA32Data()
  {
    return Data == null ? null : ImageFormatInfo.FromFormat(Format)?.ConvertToBGRA32(Data, Width, Height);
  }
}
