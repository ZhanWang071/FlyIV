namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StyleServiceStates
#else
    public class StyleServiceStates
#endif
    {
        public const string CustomStyle = "CustomStyle";
    }
}
