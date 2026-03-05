using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.looks
[assembly: InternalsVisibleTo("Genies.Looks")]
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
#endif
