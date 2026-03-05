using System.Runtime.CompilerServices;

// Same-package access
[assembly: InternalsVisibleTo("Genies.Addressables.Editor")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests")]
[assembly: InternalsVisibleTo("Genies.Addressables.Tests.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData")]
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.naf.content
[assembly: InternalsVisibleTo("Genies.Naf.Content")]
[assembly: InternalsVisibleTo("Genies.Naf.Addressables")]
#endif
