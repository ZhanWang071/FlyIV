using System.Runtime.CompilerServices;

// Same-package access
[assembly: InternalsVisibleTo("Genies.Addressables.Tests.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
#endif
