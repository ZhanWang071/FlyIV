using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.services
[assembly: InternalsVisibleTo("Genies.Avatars.Services")]
[assembly: InternalsVisibleTo("Genies.Avatars.Services.Tests.Editor")]
// com.genies.cloudsave
[assembly: InternalsVisibleTo("Genies.CloudSave")]
[assembly: InternalsVisibleTo("Genies.CloudSave.Tests")]
// com.genies.looks
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
[assembly: InternalsVisibleTo("Genies.Looks")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
// com.genies.wearable
[assembly: InternalsVisibleTo("Genies.Wearables")]
#endif
