
namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PatternTemplate
#else
    public class PatternTemplate
#endif
    {
        public ICategorizedItems<string> PatternIds;
        public ValueRange Scale;
        public Vector2ValueRange Offset;
        public ValueRange Rotation;
        public ValueRange Hue;
        public ValueRange Saturation;
        public ValueRange Gain;
        public ValueRange DuoContrast;
    }
}
