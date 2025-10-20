using System;

namespace Domain.Exceptions
{
    public class ImageUploadException : Exception
    {
        public string? PublicId { get; }
        public string? FileName { get; }

        public ImageUploadException(string message, string? publicId = null, string? fileName = null, Exception? inner = null)
            : base(message, inner)
        {
            PublicId = publicId;
            FileName = fileName;
        }
    }
}
