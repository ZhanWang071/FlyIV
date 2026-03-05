using System.Runtime.CompilerServices;

// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory.Editor")]
[assembly: InternalsVisibleTo("Genies.Inventory.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData")]
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
// com.genies.naf.content
[assembly: InternalsVisibleTo("Genies.Naf.Content")]
[assembly: InternalsVisibleTo("Genies.Naf.Addressables")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
#endif
