using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
// com.genies.naf
[assembly: InternalsVisibleTo("Genies.Naf.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
