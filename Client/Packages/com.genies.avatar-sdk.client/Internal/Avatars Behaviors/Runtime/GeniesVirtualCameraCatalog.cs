namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Catalog of Virtual Camera (Cinemachine) within the Genies Composer project
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum GeniesVirtualCameraCatalog
#else
    public enum GeniesVirtualCameraCatalog
#endif
    {
        AnimatedCamera = 0,
        FullBodyFocusCamera = 1,
        LeftSideBelowKneeFocusCamera = 2,
        LeftSideCalfFocusCamera = 3,
        LeftTopForearmFocusCamera = 4,
        LeftTopOuterArmFocusCamera = 5,
        LowerBackFocusCamera = 6,
        LowerStomachFocusCamera = 7,
        RightSideAboveTheKneeFocusCamera = 8,
        RightSideThighFocusCamera = 9,
        UpperBodyFocusCamera = 10,
        HeadFrontFocusCamera = 11,
        LowerBodyFocusCamera = 12,
        CloseUpFullBodyFocusCamera = 13,
        FrontFeetFocusCamera = 14,
    }
}
