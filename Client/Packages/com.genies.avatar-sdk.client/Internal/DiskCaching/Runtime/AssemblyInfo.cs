using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.DiskCaching.Editor.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.avatars.sdk
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]
// com.genies.currency
[assembly: InternalsVisibleTo("Genies.Currency")]
[assembly: InternalsVisibleTo("Genies.Currency.Tests.Editor")]
// com.genies.s3service
[assembly: InternalsVisibleTo("Genies.S3Service")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
#endif
