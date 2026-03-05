using System.Runtime.CompilerServices;

// com.genies.camerasystem
[assembly: InternalsVisibleTo("Genies.CameraSystem.Editor")]
[assembly: InternalsVisibleTo("Genies.CameraSystem.Tests")]
[assembly: InternalsVisibleTo("Genies.CameraSystem.Tests.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.animations
[assembly: InternalsVisibleTo("Genies.Animations")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.behaviors
[assembly: InternalsVisibleTo("Genies.Avatars.Behaviors")]
// com.genies.looks
[assembly: InternalsVisibleTo("Genies.Looks")]
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.uiframework
[assembly: InternalsVisibleTo("Genies.UIFramework")]
[assembly: InternalsVisibleTo("Genies.UIFramework.Tests.Runtime")]
[assembly: InternalsVisibleTo("Genies.UIFramework.Samples")]
#endif
