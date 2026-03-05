using System;

namespace Genies.UI.Scroller
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ScrollException : Exception
#else
    public class ScrollException : Exception
#endif
    {
        public ScrollException()
        {
        }

        public ScrollException(string message) : base(message)
        {
        }

        public ScrollException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}