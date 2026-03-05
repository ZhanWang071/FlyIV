using UnityEngine;

namespace Genies.Sdk
{
    public static class GenieColor
    {
        public const string Skin = Naf.GenieColor.Skin;

        // eyes
        public const string LeftEyeSclera      = Naf.GenieColor.LeftEyeSclera;
        public const string LeftEyeRing        = Naf.GenieColor.LeftEyeRing;
        public const string LeftEyeIrisMain    = Naf.GenieColor.LeftEyeIrisMain;
        public const string LeftEyeIrisCenter  = Naf.GenieColor.LeftEyeIrisCenter;
        public const string LeftEyePupil       = Naf.GenieColor.LeftEyePupil;
        public const string RightEyeSclera     = Naf.GenieColor.RightEyeSclera;
        public const string RightEyeRing       = Naf.GenieColor.RightEyeRing;
        public const string RightEyeIrisMain   = Naf.GenieColor.RightEyeIrisMain;
        public const string RightEyeIrisCenter = Naf.GenieColor.RightEyeIrisCenter;
        public const string RightEyePupil      = Naf.GenieColor.RightEyePupil;

        // makeup
        public const string Freckles     = Naf.GenieColor.Freckles;
        public const string BlushAll     = Naf.GenieColor.BlushAll;
        public const string Blush1       = Naf.GenieColor.Blush1;
        public const string Blush2       = Naf.GenieColor.Blush2;
        public const string Blush3       = Naf.GenieColor.Blush3;
        public const string LipstickAll  = Naf.GenieColor.LipstickAll;
        public const string Lipstick1    = Naf.GenieColor.Lipstick1;
        public const string Lipstick2    = Naf.GenieColor.Lipstick2;
        public const string Lipstick3    = Naf.GenieColor.Lipstick3;
        public const string EyeshadowAll = Naf.GenieColor.EyeshadowAll;
        public const string Eyeshadow1   = Naf.GenieColor.Eyeshadow1;
        public const string Eyeshadow2   = Naf.GenieColor.Eyeshadow2;
        public const string Eyeshadow3   = Naf.GenieColor.Eyeshadow3;
        public const string FaceGemsAll  = Naf.GenieColor.FaceGemsAll;
        public const string FaceGems1    = Naf.GenieColor.FaceGems1;
        public const string FaceGems2    = Naf.GenieColor.FaceGems2;
        public const string FaceGems3    = Naf.GenieColor.FaceGems3;

        // hair
        public const string HairAll  = Naf.GenieColor.HairAll;
        public const string HairBase = Naf.GenieColor.HairBase;
        public const string HairR    = Naf.GenieColor.HairR;
        public const string HairG    = Naf.GenieColor.HairG;
        public const string HairB    = Naf.GenieColor.HairB;

        // facial hair
        public const string FacialhairAll  = Naf.GenieColor.FacialhairAll;
        public const string FacialhairBase = Naf.GenieColor.FacialhairBase;
        public const string FacialhairR    = Naf.GenieColor.FacialhairR;
        public const string FacialhairG    = Naf.GenieColor.FacialhairG;
        public const string FacialhairB    = Naf.GenieColor.FacialhairB;

        // eyebrows
        public const string EyebrowsAll  = Naf.GenieColor.EyebrowsAll;
        public const string EyebrowsBase = Naf.GenieColor.EyebrowsBase;
        public const string EyebrowsR    = Naf.GenieColor.EyebrowsR;
        public const string EyebrowsG    = Naf.GenieColor.EyebrowsG;
        public const string EyebrowsB    = Naf.GenieColor.EyebrowsB;

        // eyelashes
        public const string EyelashesAll  = Naf.GenieColor.EyelashesAll;
        public const string EyelashesBase = Naf.GenieColor.EyelashesBase;
        public const string EyelashesR    = Naf.GenieColor.EyelashesR;
        public const string EyelashesG    = Naf.GenieColor.EyelashesG;
        public const string EyelashesB    = Naf.GenieColor.EyelashesB;
    }

    public struct GenieColorEntry
    {
        public string ColorId;
        public Color? Value;

        public GenieColorEntry(string colorId = null, Color? value = null)
        {
            ColorId = colorId;
            Value   = value;
        }

        internal static Naf.GenieColorEntry ToNaf(GenieColorEntry entry)
        {
            return new Naf.GenieColorEntry(entry.ColorId, entry.Value);
        }

        internal static GenieColorEntry FromNaf(Naf.GenieColorEntry entry)
        {
            return new GenieColorEntry(entry.ColorId, entry.Value);
        }
    }
}
