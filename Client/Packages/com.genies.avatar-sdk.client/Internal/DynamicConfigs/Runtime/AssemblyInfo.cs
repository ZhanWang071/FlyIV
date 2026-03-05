using System.Runtime.CompilerServices;

// Same-package access
[assembly: InternalsVisibleTo("Genies.Services.DynamicConfigs.Editor")]
[assembly: InternalsVisibleTo("Genies.Services.DynamicConfigs.Tests")]
[assembly: InternalsVisibleTo("Genies.Services.DynamicConfigs.Tests.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
#endif
