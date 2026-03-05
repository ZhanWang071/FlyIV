using System.Runtime.CompilerServices;

// Genies - same package
[assembly: InternalsVisibleTo("Genies.Components.ShaderlessTools.Editor")]
[assembly: InternalsVisibleTo("Genies.ShaderlessTools.Editor.Tests")]
[assembly: InternalsVisibleTo("Genies.ShaderlessTools.Tests")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.assets.services
[assembly: InternalsVisibleTo("Genies.Assets.Services")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.GenieComponentsSample")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.datamodels
[assembly: InternalsVisibleTo("Genies.Components.DataModels")]
[assembly: InternalsVisibleTo("UMA_Core")]
#endif
