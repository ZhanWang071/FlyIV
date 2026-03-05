using System;
using UnityEngine;

namespace Genies.Avatars
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SerializableHumanLimit
#else
    public struct SerializableHumanLimit
#endif
    {
        public bool    useDefaultValues;
        public Vector3 min;
        public Vector3 max;
        public Vector3 center;
        public float   axisLength;
        
        public static SerializableHumanLimit Convert(HumanLimit humanLimit)
        {
            return new SerializableHumanLimit
            {
                useDefaultValues = humanLimit.useDefaultValues,
                min              = humanLimit.min,
                max              = humanLimit.max,
                center           = humanLimit.center,
                axisLength       = humanLimit.axisLength,
            };
        }
        
        public static HumanLimit Convert(SerializableHumanLimit humanLimit)
        {
            return new HumanLimit
            {
                useDefaultValues = humanLimit.useDefaultValues,
                min              = humanLimit.min,
                max              = humanLimit.max,
                center           = humanLimit.center,
                axisLength       = humanLimit.axisLength,
            };
        }
    }
}