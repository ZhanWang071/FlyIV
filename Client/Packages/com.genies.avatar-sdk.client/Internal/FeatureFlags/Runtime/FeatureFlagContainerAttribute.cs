using System;

namespace Genies.FeatureFlags
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FeatureFlagsContainerAttribute : Attribute
#else
    public class FeatureFlagsContainerAttribute : Attribute
#endif
    {
        public int Order { get; }

        public FeatureFlagsContainerAttribute(int order = -1)
        {
            Order = order;
        }
    }
}
