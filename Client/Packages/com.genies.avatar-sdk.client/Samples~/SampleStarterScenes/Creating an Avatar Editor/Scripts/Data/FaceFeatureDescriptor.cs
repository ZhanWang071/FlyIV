namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Data class describing a face feature for UI display and generation
    /// </summary>
    public class FaceFeatureDescriptor
    {
        public string DisplayName { get; }
        public bool SupportsStats { get; }
        public UIGeneratorGroup GeneratorGroup { get; }

        public FaceFeatureDescriptor(
            string displayName,
            bool supportsStats,
            UIGeneratorGroup generatorGroup)
        {
            DisplayName = displayName;
            SupportsStats = supportsStats;
            GeneratorGroup = generatorGroup;
        }
    }
}

