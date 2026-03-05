using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.GenieComponentsSample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
// com.genies.avatars.behaviors
[assembly: InternalsVisibleTo("Genies.Avatars.Behaviors")]
// com.genies.cms
[assembly: InternalsVisibleTo("Genies.Cms")]
[assembly: InternalsVisibleTo("Genies.Cms.AnimationLibrary")]
[assembly: InternalsVisibleTo("Genies.Cms.Animations")]
[assembly: InternalsVisibleTo("Genies.Cms.Avatars")]
[assembly: InternalsVisibleTo("Genies.Cms.Editor")]
[assembly: InternalsVisibleTo("Genies.Cms.Presets")]
[assembly: InternalsVisibleTo("Genies.Cms.ResourceLocationProvider")]
[assembly: InternalsVisibleTo("Genies.Cms.Scenes")]
[assembly: InternalsVisibleTo("Genies.Cms.Tests")]
[assembly: InternalsVisibleTo("Genies.Cms.Things")]
[assembly: InternalsVisibleTo("Genies.Cms.Traits")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
#endif
