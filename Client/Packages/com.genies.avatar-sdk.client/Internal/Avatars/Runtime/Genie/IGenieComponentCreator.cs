namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IGenieComponentCreator
#else
    public interface IGenieComponentCreator
#endif
    {
        GenieComponent CreateComponent();
    }
}