using Newtonsoft.Json;

namespace Genies.UGCW.Data.DecoratedSkin
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class DecoratedSkinExtensions 
#else
    public static class DecoratedSkinExtensions 
#endif
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };

        public static string SerializeDefinition(this DecoratedSkinDefinition def)
        {
            return JsonConvert.SerializeObject(def, SerializerSettings);
        }
        
        public static DecoratedSkinDefinition DefaultDefinition()
        {
            var def = new DecoratedSkinDefinition
            {
                BaseSkin = new BaseSkinDefinition(),
                Makeup =  new MakeupDefinition(),
                // 8 = number of places that the user can put tattoos in the avatar bodypart
                Tattoos = new TattooDefinition[8]
            };
            return def;
        }
    }
}