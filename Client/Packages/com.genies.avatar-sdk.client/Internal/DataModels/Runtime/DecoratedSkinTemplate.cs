using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DecoratedSkinTemplate : OrderedScriptableObject
#else
    public class DecoratedSkinTemplate : OrderedScriptableObject
#endif
    {
        [SerializeField] private Texture2D _map;
        [SerializeField] private Texture2D _icon;

        public Texture2D Map
        {
            get => _map;
            set => _map = value;
        }
        public Texture2D Icon
        {
            get => _icon;
            set => _icon = value;
        }
    }
}
