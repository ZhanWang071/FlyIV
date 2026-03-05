using System;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Exception thrown when an invalid avatar modification is attempted
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class InvalidAvatarModificationException : Exception
#else
    public class InvalidAvatarModificationException : Exception
#endif
    {
        public InvalidAvatarModificationException(string message) : base(message)
        {
        }
    }
}