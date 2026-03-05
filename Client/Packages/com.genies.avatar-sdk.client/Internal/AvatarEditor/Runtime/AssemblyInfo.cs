using System.Runtime.CompilerServices;

// com.genies.sdk.avatar.telemetry
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
#endif
