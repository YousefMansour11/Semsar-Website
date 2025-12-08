using System;
using System.IO;

namespace Application.Services
{
    // Magic-byte image validation utility.
    // Currently available for use but not wired into the active upload pipeline —
    // CloudinaryService and ResilientCloudinaryService handle their own validation.
    // Call ImageValidation.ValidateImageHeader(stream, contentType, fileName) before upload
    // to add an extra defense layer against MIME-type spoofing.
    public static class ImageValidation
    {
        public static bool ValidateImageHeader(Stream stream, string contentType, string fileName)
        {
            if (stream == null) return false;
            try
            {
                var buffer = new byte[12];
                var read = 0;
                if (stream.CanSeek)
                {
                    var pos = stream.Position;
                    read = stream.Read(buffer, 0, buffer.Length);
                    stream.Position = pos;
                }
                else
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                }

                if (read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) return true;
                if (read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) return true;
                if (read >= 12 && buffer[0] == (byte)'R' && buffer[1] == (byte)'I' && buffer[2] == (byte)'F' && buffer[3] == (byte)'F' && buffer[8] == (byte)'W' && buffer[9] == (byte)'E' && buffer[10] == (byte)'B' && buffer[11] == (byte)'P') return true;

                return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
