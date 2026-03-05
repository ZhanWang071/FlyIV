using System.Runtime.CompilerServices;

// com.genies.dynamics
[assembly: InternalsVisibleTo("Genies.Dynamics.Editor")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
#endif
