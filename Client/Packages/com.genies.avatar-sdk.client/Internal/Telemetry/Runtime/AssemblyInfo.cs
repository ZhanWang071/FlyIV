using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.Telemetry.Editor")]

// com.genies.sdk.avatar.telemetry
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
#endif
