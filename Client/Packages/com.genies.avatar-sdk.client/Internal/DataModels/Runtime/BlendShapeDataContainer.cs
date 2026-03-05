using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BlendShapeType
#else
    public enum BlendShapeType
#endif
    {
        None = 0,
        Eyes = 1,
        Jaw = 2,
        Lips = 3,
        Nose = 4,
        Brow = 5
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BlendShapeTag
#else
    public enum BlendShapeTag
#endif
    {
        Gen4 = 0,
        Silver = 1
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DNAItem //Unity serializable pair
#else
    public class DNAItem //Unity serializable pair
#endif
    {
        public string Name;
        public float Value;
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapeDataContainer", menuName = "Blendshapes/BlendShapeDataContainer")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BlendShapeDataContainer : OrderedScriptableObject
#else
    public class BlendShapeDataContainer : OrderedScriptableObject
#endif
    {
        public BlendShapeType Type = BlendShapeType.None;
        public BlendShapeTag Tag = BlendShapeTag.Gen4;
        public Texture2D maleIcon;
        public Texture2D femaleIcon;
        public Texture2D unifiedIcon;
        public string blendShapeIdentifier;
        public List<DNAItem> DNA;
    }
}
