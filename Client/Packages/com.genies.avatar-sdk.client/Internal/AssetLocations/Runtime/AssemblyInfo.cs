using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
// com.genies.avatareditor
[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
// com.genies.inventory
[assembly: InternalsVisibleTo("Genies.Inventory")]
// com.genies.naf.content
[assembly: InternalsVisibleTo("Genies.Naf.Content")]
[assembly: InternalsVisibleTo("Genies.Naf.Addressables")]
#endif
