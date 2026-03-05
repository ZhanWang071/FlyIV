using System;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct BodyAttributeState
#else
    public struct BodyAttributeState
#endif
    {
        public string name;
        [Range(-1.0f, 1.0f)]
        public float weight;

        public BodyAttributeState(string name, float weight)
        {
            this.name = name;
            this.weight = weight;
        }
    }
}