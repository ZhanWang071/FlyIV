namespace Genies.Animations
{
    /// <summary>
    /// Catalog of Virtual Camera (Cinemachine) within the Genies Animation package
    /// </summary>
    /// <seealso cref="GeniesVirtualCamera"/>
    /// <seealso cref="LookAnimationController"/>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AnimationVirtualCameraCatalog
#else
    public enum AnimationVirtualCameraCatalog
#endif
    {
        /// <summary>
        /// Option to set the main camera to follow the animated Camera (with animation) under the avatar object
        /// </summary>
        AnimatedCamera = 0,

        /// <summary>
        /// Option to have the main camera focus on the full body
        /// </summary>
        FullBodyFocusCamera = 1,
    }
}


