using System;
using System.Linq;

namespace Application.Services
{
    public static class SeoUtils
    {
        public static bool ContainsArabic(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }
    }
}
