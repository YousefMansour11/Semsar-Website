namespace Application.Services;

public static class ImageHeaderParser
{
    public static (int Width, int Height) GetDimensions(Stream stream, string extension)
    {
        try
        {
            var ext = extension.TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "jpg" or "jpeg" => ReadJpegDimensions(stream),
                "png" => ReadPngDimensions(stream),
                "webp" => ReadWebpDimensions(stream),
                _ => (0, 0),
            };
        }
        catch
        {
            return (0, 0);
        }
    }

    public static double EstimateMegapixels(int width, int height) =>
        width > 0 && height > 0 ? Math.Round(width * height / 1_000_000.0, 2) : 0;

    public static double EstimateCompressionRatio(long fileSizeBytes, int width, int height) =>
        width > 0 && height > 0 ? Math.Round(fileSizeBytes / (double)(width * height), 4) : 0;

    private static void ReadExact(Stream stream, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read <= 0) throw new EndOfStreamException();
            offset += read;
        }
    }

    private static (int Width, int Height) ReadJpegDimensions(Stream stream)
    {
        if (stream.Length < 4) return (0, 0);
        var pos = stream.Position;
        stream.Position = 0;

        var buf = new byte[2];
        ReadExact(stream, buf, 2);
        if (buf[0] != 0xFF || buf[1] != 0xD8) { stream.Position = pos; return (0, 0); }

        while (stream.Position < stream.Length)
        {
            var markerBuf = new byte[4];
            if (stream.Read(markerBuf, 0, 4) < 4) break;
            if (markerBuf[0] != 0xFF) break;

            if (markerBuf[1] == 0xC0 || markerBuf[1] == 0xC1 || markerBuf[1] == 0xC2)
            {
                var segBuf = new byte[3];
                ReadExact(stream, segBuf, 3);
                var h = (segBuf[1] << 8) | segBuf[2];
                var wBuf = new byte[2];
                ReadExact(stream, wBuf, 2);
                var w = (wBuf[0] << 8) | wBuf[1];
                stream.Position = pos;
                return (w, h);
            }

            var segLen = (markerBuf[2] << 8) | markerBuf[3];
            if (segLen < 2) break;
            stream.Seek(segLen - 2, SeekOrigin.Current);
        }

        stream.Position = pos;
        return (0, 0);
    }

    private static (int Width, int Height) ReadPngDimensions(Stream stream)
    {
        if (stream.Length < 24) return (0, 0);
        var pos = stream.Position;
        stream.Position = 0;

        var sig = new byte[8];
        ReadExact(stream, sig, 8);
        if (sig[0] != 0x89 || sig[1] != 0x50 || sig[2] != 0x4E || sig[3] != 0x47) { stream.Position = pos; return (0, 0); }

        var ihdr = new byte[4];
        ReadExact(stream, ihdr, 4);
        var len = (ihdr[0] << 24) | (ihdr[1] << 16) | (ihdr[2] << 8) | ihdr[3];
        if (len < 13) { stream.Position = pos; return (0, 0); }

        var type = new byte[4];
        ReadExact(stream, type, 4);
        if (type[0] != 0x49 || type[1] != 0x48 || type[2] != 0x44 || type[3] != 0x52) { stream.Position = pos; return (0, 0); }

        var wh = new byte[8];
        ReadExact(stream, wh, 8);
        var w = (wh[0] << 24) | (wh[1] << 16) | (wh[2] << 8) | wh[3];
        var h = (wh[4] << 24) | (wh[5] << 16) | (wh[6] << 8) | wh[7];

        stream.Position = pos;
        return (w, h);
    }

    private static (int Width, int Height) ReadWebpDimensions(Stream stream)
    {
        if (stream.Length < 30) return (0, 0);
        var pos = stream.Position;
        stream.Position = 0;

        var riff = new byte[4];
        ReadExact(stream, riff, 4);
        if (riff[0] != (byte)'R' || riff[1] != (byte)'I' || riff[2] != (byte)'F' || riff[3] != (byte)'F') { stream.Position = pos; return (0, 0); }

        stream.Seek(4, SeekOrigin.Current);

        var webp = new byte[4];
        ReadExact(stream, webp, 4);
        if (webp[0] != (byte)'W' || webp[1] != (byte)'E' || webp[2] != (byte)'B' || webp[3] != (byte)'P') { stream.Position = pos; return (0, 0); }

        var chunkType = new byte[4];
        ReadExact(stream, chunkType, 4);

        var typeStr = System.Text.Encoding.ASCII.GetString(chunkType);

        if (typeStr == "VP8 " || typeStr == "VP8L")
        {
            stream.Seek(4, SeekOrigin.Current);
            var vp8Buf = new byte[4];
            ReadExact(stream, vp8Buf, 4);
            var w = (vp8Buf[1] & 0x3F) << 8 | vp8Buf[0];
            var h = (vp8Buf[3] & 0x3F) << 8 | vp8Buf[2];
            stream.Position = pos;
            return (w, h);
        }

        if (typeStr == "VP8X")
        {
            stream.Seek(4, SeekOrigin.Current);
            var vp8xBuf = new byte[10];
            ReadExact(stream, vp8xBuf, 10);
            var w = ((vp8xBuf[6] << 16) | (vp8xBuf[5] << 8) | vp8xBuf[4]) + 1;
            var h = ((vp8xBuf[9] << 16) | (vp8xBuf[8] << 8) | vp8xBuf[7]) + 1;
            stream.Position = pos;
            return (w, h);
        }

        stream.Position = pos;
        return (0, 0);
    }
}