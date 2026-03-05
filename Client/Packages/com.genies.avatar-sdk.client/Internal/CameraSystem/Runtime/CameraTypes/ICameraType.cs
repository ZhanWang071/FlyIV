namespace Genies.CameraSystem
{
    /// <summary>
    /// Interface for setting up new virtual camera classes.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICameraType
#else
    public interface ICameraType
#endif
    {
        /// <summary>
        /// Sets up components and dependencies for the camera
        /// </summary>
        public void ConfigureVirtualCamera();
        public void ToggleBehaviour(bool value);
    }
}
