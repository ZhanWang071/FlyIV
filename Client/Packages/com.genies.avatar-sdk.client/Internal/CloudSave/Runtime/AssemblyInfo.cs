using System.Runtime.CompilerServices;

// Same-package access
[assembly: InternalsVisibleTo("Genies.CloudSave.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.services
[assembly: InternalsVisibleTo("Genies.Avatars.Services")]
[assembly: InternalsVisibleTo("Genies.Avatars.Services.Tests.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
