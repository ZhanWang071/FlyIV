using System;

namespace RectpackSharp
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PackingException : Exception
#else
    public class PackingException : Exception
#endif
    {
        public PackingException() : base() { }

        public PackingException(string message) : base(message) { }

        public PackingException(string message,  Exception innerException) : base(message, innerException) { }
    }
}
