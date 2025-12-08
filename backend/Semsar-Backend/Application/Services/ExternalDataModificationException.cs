using System;

namespace Application.Services
{
    public class ExternalDataModificationException : Exception
    {
        public ExternalDataModificationException(string message) : base(message) { }
    }
}
