namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Holds information about the currently active face feature being edited
    /// </summary>
    public class FaceEditingState
    {
        public FaceFeatureDescriptor ActiveFeature { get; private set; }

        public void SetFeature(FaceFeatureDescriptor feature)
        {
            ActiveFeature = feature;
        }
    }
}
