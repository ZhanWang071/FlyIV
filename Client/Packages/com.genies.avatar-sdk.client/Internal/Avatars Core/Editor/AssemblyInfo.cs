using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.experience.gameplay
[assembly: InternalsVisibleTo("Genies.Experience.Gameplay.Editor")]
// com.genies.multiplayer
[assembly: InternalsVisibleTo("Genies.Multiplayer.Editor")]
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
// com.genies.sdk.avatar.telemetry
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Telemetry.Editor")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
#endif
