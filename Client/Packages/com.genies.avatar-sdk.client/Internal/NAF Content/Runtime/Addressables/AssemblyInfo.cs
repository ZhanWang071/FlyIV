using System.Runtime.CompilerServices;

// com.genies.naf.content
[assembly: InternalsVisibleTo("Genies.Naf.Content")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.sdk.avatar
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar.Editor")]
// com.genies.sdk.core
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core.Editor")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
[assembly: InternalsVisibleTo("SilverStudioLookDemo")]
[assembly: InternalsVisibleTo("SilverStudioBase")]
[assembly: InternalsVisibleTo("SilverStudioAvatarDemo")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.looks
[assembly: InternalsVisibleTo("Genies.Looks")]
[assembly: InternalsVisibleTo("Com.Genies.Looks.Tests.Editor")]
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData")]
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
// com.genies.wearable
[assembly: InternalsVisibleTo("Genies.Wearables")]
#endif
