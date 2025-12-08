using System;

namespace Application.Services
{
    public class SlugConflictException : Exception
    {
        public SlugConflictException(string message) : base(message) { }
        public SlugConflictException(string message, Exception? inner) : base(message, inner) { }
    }
}
