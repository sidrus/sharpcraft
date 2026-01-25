namespace SharpCraft.Engine.Rendering.IBL;

/// <summary>
/// Loads HDR (Radiance RGBE format) textures for IBL.
/// Supports .hdr files in the Radiance RGBE format.
/// </summary>
public static class HdrTextureLoader
{
    /// <summary>
    /// Loads an HDR texture from a file path.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="path">Path to the .hdr file</param>
    /// <returns>OpenGL texture handle</returns>
    public static uint LoadHdrTexture(GL gl, string path)
    {
        using var stream = File.OpenRead(path);
        return LoadHdrTexture(gl, stream);
    }

    /// <summary>
    /// Loads an HDR texture from a stream.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="stream">Stream containing HDR data</param>
    /// <returns>OpenGL texture handle</returns>
    public static uint LoadHdrTexture(GL gl, Stream stream)
    {
        var (pixels, width, height) = LoadHdrData(stream);
        return CreateHdrTexture(gl, pixels, width, height);
    }

    /// <summary>
    /// Creates an HDR texture from raw float RGB data.
    /// </summary>
    public static uint CreateHdrTexture(GL gl, float[] pixels, int width, int height)
    {
        var texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);

        unsafe
        {
            fixed (float* p = pixels)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb16f,
                    (uint)width, (uint)height, 0, PixelFormat.Rgb, PixelType.Float, p);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        gl.BindTexture(TextureTarget.Texture2D, 0);

        return texture;
    }

    /// <summary>
    /// Loads HDR data from a Radiance RGBE format stream.
    /// </summary>
    private static (float[] pixels, int width, int height) LoadHdrData(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        // Read header
        var header = ReadLine(reader);
        if (!header.StartsWith("#?RADIANCE") && !header.StartsWith("#?RGBE"))
        {
            throw new InvalidDataException("Not a valid HDR file");
        }

        // Skip header lines until we find the format
        string line;
        var format = "";
        while ((line = ReadLine(reader)) != "")
        {
            if (line.StartsWith("FORMAT="))
            {
                format = line[7..];
            }
        }

        if (format != "32-bit_rle_rgbe" && format != "32-bit_rle_xyze")
        {
            // Try to continue anyway for simple formats
        }

        // Read resolution
        var resolution = ReadLine(reader);
        var parts = resolution.Split(' ');
        
        int width, height;
        if (parts.Length >= 4 && parts[0] == "-Y" && parts[2] == "+X")
        {
            height = int.Parse(parts[1]);
            width = int.Parse(parts[3]);
        }
        else if (parts.Length >= 4 && parts[0] == "+Y" && parts[2] == "+X")
        {
            height = int.Parse(parts[1]);
            width = int.Parse(parts[3]);
        }
        else
        {
            throw new InvalidDataException($"Unsupported HDR resolution format: {resolution}");
        }

        // Read pixel data
        var pixels = new float[width * height * 3];
        var scanline = new byte[width * 4];

        for (var y = 0; y < height; y++)
        {
            ReadScanline(reader, scanline, width);

            for (var x = 0; x < width; x++)
            {
                var r = scanline[x];
                var g = scanline[width + x];
                var b = scanline[width * 2 + x];
                var e = scanline[width * 3 + x];

                var idx = (y * width + x) * 3;
                if (e != 0)
                {
                    var exp = MathF.Pow(2.0f, e - 128 - 8);
                    pixels[idx] = r * exp;
                    pixels[idx + 1] = g * exp;
                    pixels[idx + 2] = b * exp;
                }
                else
                {
                    pixels[idx] = 0;
                    pixels[idx + 1] = 0;
                    pixels[idx + 2] = 0;
                }
            }
        }

        return (pixels, width, height);
    }

    private static void ReadScanline(BinaryReader reader, byte[] scanline, int width)
    {
        // Check for new RLE format
        var first = reader.ReadByte();
        var second = reader.ReadByte();

        if (first != 2 || second != 2)
        {
            // Old format - read directly
            scanline[0] = first;
            scanline[1] = second;
            reader.Read(scanline, 2, width * 4 - 2);
            
            // Reorganize from RGBE interleaved to planar
            var temp = new byte[width * 4];
            Array.Copy(scanline, temp, width * 4);
            for (var i = 0; i < width; i++)
            {
                scanline[i] = temp[i * 4];
                scanline[width + i] = temp[i * 4 + 1];
                scanline[width * 2 + i] = temp[i * 4 + 2];
                scanline[width * 3 + i] = temp[i * 4 + 3];
            }
            return;
        }

        // Read width from scanline header
        var widthHigh = reader.ReadByte();
        var widthLow = reader.ReadByte();
        var scanlineWidth = (widthHigh << 8) | widthLow;

        if (scanlineWidth != width)
        {
            throw new InvalidDataException("Scanline width mismatch");
        }

        // Read each channel with RLE
        for (var channel = 0; channel < 4; channel++)
        {
            var offset = channel * width;
            var count = 0;

            while (count < width)
            {
                var code = reader.ReadByte();

                if (code > 128)
                {
                    // Run
                    var runLength = code - 128;
                    var value = reader.ReadByte();
                    for (var i = 0; i < runLength; i++)
                    {
                        scanline[offset + count++] = value;
                    }
                }
                else
                {
                    // Literal
                    for (var i = 0; i < code; i++)
                    {
                        scanline[offset + count++] = reader.ReadByte();
                    }
                }
            }
        }
    }

    private static string ReadLine(BinaryReader reader)
    {
        var chars = new List<char>();
        while (true)
        {
            var b = reader.ReadByte();
            if (b == '\n') break;
            if (b != '\r') chars.Add((char)b);
        }
        return new string(chars.ToArray());
    }
}
