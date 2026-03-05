using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class OrderedScriptableObject : ScriptableObject
#else
    public class OrderedScriptableObject : ScriptableObject
#endif
    {
        public int Order;
    }
}
