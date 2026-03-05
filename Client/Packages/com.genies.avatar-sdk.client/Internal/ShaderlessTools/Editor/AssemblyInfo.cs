using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.ShaderlessTools.Editor.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
#endif
