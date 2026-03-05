
namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ITemplateValidator
#else
    public interface ITemplateValidator
#endif
    {
        void ValidateWearable(Wearable wearable, WearableTemplate template, bool validateSubModels = true);
        void ValidateSplit(Split split, SplitTemplate template, bool validateSubModels = true);
        void ValidateRegion(Region region, RegionTemplate template, bool validateSubModels = true);
        void ValidateStyle(Style style, StyleTemplate template, bool validateSubModels = true);
        void ValidatePattern(Pattern pattern, PatternTemplate template);
        Style CreateDefaultStyle();
        Pattern CreateDefaultPattern();
    }
}
