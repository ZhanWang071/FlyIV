using UnityEngine;

namespace Genies.UIFramework.Widgets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PictureInPictureCameraProvider
#else
    public class PictureInPictureCameraProvider
#endif
    {
        public Camera Camera { get; }

        public PictureInPictureCameraProvider(Camera camera)
        {
            this.Camera = camera;
        }
    }
}
