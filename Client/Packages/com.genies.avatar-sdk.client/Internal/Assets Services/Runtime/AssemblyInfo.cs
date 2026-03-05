using System.Runtime.CompilerServices;

// Same-package access
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests")]
[assembly: InternalsVisibleTo("Genies.Assets.Services.Tests.Editor")]

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.animations
[assembly: InternalsVisibleTo("Genies.Animations")]
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.avatars
[assembly: InternalsVisibleTo("Genies.Avatars")]
[assembly: InternalsVisibleTo("Genies.Avatars.Tests.Editor")]
// com.genies.avatars.context
[assembly: InternalsVisibleTo("Genies.Avatars.Context")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
// com.genies.inventory.uidata
[assembly: InternalsVisibleTo("Genies.Inventory.UIData")]
[assembly: InternalsVisibleTo("Genies.Inventory.UIData.Editor")]
// com.genies.megaeditor
[assembly: InternalsVisibleTo("Genies.MegaEditor")]
// com.genies.ugc
[assembly: InternalsVisibleTo("Genies.Ugc")]
[assembly: InternalsVisibleTo("Genies.Ugc.Editor.Tests")]
#endif
