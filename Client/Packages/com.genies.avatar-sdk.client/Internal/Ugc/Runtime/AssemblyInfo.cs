using System.Runtime.CompilerServices;

// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.wearable
[assembly: InternalsVisibleTo("Genies.Wearables")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
#endif
