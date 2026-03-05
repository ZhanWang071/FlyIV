using System.Collections.Generic;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains a group of items organized by categories, where each item has also a display name.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICategorizedItems<T>
#else
    public interface ICategorizedItems<T>
#endif
    {
        List<string> Categories { get; }
        string DefaultCategory { get; }

        IReadOnlyList<T> GetItems(string category = null); // null category should return all items
        int GetItemCount(string category = null);
        bool ContainsItem(T item, string category = null);
        string GetItemDisplayName(T item);
        string GetItemCategory(T item);
        T GetDefaultItem(string category = null);
    }
}
