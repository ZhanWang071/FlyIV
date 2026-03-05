using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class BodyControllerExtensions
#else
    public static class BodyControllerExtensions
#endif
    {
        public static bool IsPresetApplied(this IBodyController controller, IReadOnlyDictionary<string, float> preset)
        {
            foreach ((string attribute, float weight) in preset)
            {
                if (!Mathf.Approximately(controller.GetAttributeWeight(attribute), weight))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public static bool IsPresetApplied(this IBodyController controller, IEnumerable<BodyAttributeState> preset)
        {
            foreach (BodyAttributeState attributeState in preset)
            {
                if (!Mathf.Approximately(controller.GetAttributeWeight(attributeState.name), attributeState.weight))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}