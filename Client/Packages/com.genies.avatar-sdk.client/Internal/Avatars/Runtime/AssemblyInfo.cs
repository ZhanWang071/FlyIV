using System.Runtime.CompilerServices;

// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.GenieComponentsSample")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.behaviors
[assembly: InternalsVisibleTo("Genies.Avatars.Behaviors")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.avatars.services
[assembly: InternalsVisibleTo("Genies.Avatars.Services")]
[assembly: InternalsVisibleTo("Genies.Avatars.Services.Tests.Editor")]
// com.genies.camerasystem
[assembly: InternalsVisibleTo("Genies.CameraSystem")]
// com.genies.dynamics
[assembly: InternalsVisibleTo("Genies.Dynamics")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Tests")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Editor")]
// com.genies.looks
[assembly: InternalsVisibleTo("Genies.Looks")]
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.naf
[assembly: InternalsVisibleTo("Genies.Naf")]
[assembly: InternalsVisibleTo("Genies.Naf.Editor")]
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
