using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.Components.Shaders.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.GenieComponentsSample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
// com.genies.naf
[assembly: InternalsVisibleTo("Genies.Naf")]
[assembly: InternalsVisibleTo("Genies.Naf.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
