using System.Runtime.CompilerServices;

// com.genies.megaeditor
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
#endif
