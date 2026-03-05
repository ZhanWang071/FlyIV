using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.avatars.services
[assembly: InternalsVisibleTo("Genies.Avatars.Services.Tests.Editor")]
// com.genies.cms
[assembly: InternalsVisibleTo("Genies.Cms.Editor")]
// com.genies.currency
[assembly: InternalsVisibleTo("Genies.Currency.Tests.Editor")]
// com.genies.experience.cloudsave
[assembly: InternalsVisibleTo("Genies.Experience.CloudSave.Tests.Editor")]
// com.genies.experience.sdk
[assembly: InternalsVisibleTo("Genies.Experience.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Experience.Sdk.Tests.Editor")]
// com.genies.leaderboard
[assembly: InternalsVisibleTo("Genies.Leaderboard.Tests.Editor")]
// com.genies.looks
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
// com.genies.multiplayer
[assembly: InternalsVisibleTo("Genies.Multiplayer.Editor")]
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
// com.genies.telemetry
[assembly: InternalsVisibleTo("Genies.Telemetry.Editor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
