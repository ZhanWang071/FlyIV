using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.sdk.avatar.telemetry
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry.Editor")]
#endif
