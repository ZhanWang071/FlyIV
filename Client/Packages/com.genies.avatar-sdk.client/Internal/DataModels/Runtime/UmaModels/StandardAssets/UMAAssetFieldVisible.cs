namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMAAssetFieldVisible : System.Attribute { }
#else
	public class UMAAssetFieldVisible : System.Attribute { }
#endif
}
