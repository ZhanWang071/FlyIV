using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.cloudsave
[assembly: InternalsVisibleTo("Genies.CloudSave")]
[assembly: InternalsVisibleTo("Genies.CloudSave.Tests")]
// com.genies.looks
[assembly: InternalsVisibleTo("Genies.Looks")]
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
// com.genies.wearable
[assembly: InternalsVisibleTo("Genies.Wearables")]
#endif
