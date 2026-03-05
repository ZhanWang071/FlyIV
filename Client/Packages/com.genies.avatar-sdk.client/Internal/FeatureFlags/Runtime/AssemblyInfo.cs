using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.addressables
[assembly: InternalsVisibleTo("Genies.Addressables")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests.Editor")]
[assembly: InternalsVisibleTo("Genies.Addressables.Editor")]
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.GenieComponentsSample")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.dynamics
[assembly: InternalsVisibleTo("Genies.Dynamics")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Tests")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Editor")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
