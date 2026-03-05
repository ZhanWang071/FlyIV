using System.Runtime.CompilerServices;

// com.genies.datamodels
[assembly: InternalsVisibleTo("UMA_Core")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.addressables
[assembly: InternalsVisibleTo("Genies.Addressables")]
[assembly: InternalsVisibleTo("Genies.Addressables.Editor")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests.Editor")]
// com.genies.animations
[assembly: InternalsVisibleTo("Genies.Animations")]
// com.genies.assetlocations
[assembly: InternalsVisibleTo("Genies.AssetLocations")]
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
[assembly: InternalsVisibleTo("Genies.Components.Avatars.GenieComponentsSample")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.creatortools.utils
[assembly: InternalsVisibleTo("Genies.CreatorTools.utils")]
// com.genies.dynamics
[assembly: InternalsVisibleTo("Genies.Dynamics")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Editor")]
[assembly: InternalsVisibleTo("Genies.Dynamics.Tests")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData")]
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
// com.genies.naf.content
[assembly: InternalsVisibleTo("Genies.Naf.Content")]
[assembly: InternalsVisibleTo("Genies.Naf.Addressables")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
