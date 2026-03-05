using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.UIFramework.Samples")]
[assembly: InternalsVisibleTo("Genies.UIFramework.Tests.Runtime")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.behaviors
[assembly: InternalsVisibleTo("Genies.Avatars.Behaviors")]
// com.genies.currency
[assembly: InternalsVisibleTo("Genies.Currency")]
[assembly: InternalsVisibleTo("Genies.Currency.Tests.Editor")]
// com.genies.customizer
[assembly: InternalsVisibleTo("Genies.Customizer")]
[assembly: InternalsVisibleTo("Genies.Customizer.Editor")]
// com.genies.leaderboard
[assembly: InternalsVisibleTo("Genies.Leaderboard")]
[assembly: InternalsVisibleTo("Genies.Leaderboard.Tests.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
#endif
