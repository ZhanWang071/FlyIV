using System;
using System.Runtime.Serialization;

namespace Genies.Avatars
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class AvatarBodyDeformException : Exception
#else
    public sealed class AvatarBodyDeformException : Exception
#endif
    {
        public AvatarBodyDeformException()
        {
        }

        public AvatarBodyDeformException(string message) : base(message)
        {
        }

        public AvatarBodyDeformException(string message, Exception inner) : base(message, inner)
        {
        }

        public AvatarBodyDeformException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}