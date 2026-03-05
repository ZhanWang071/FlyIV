using UnityEngine;

namespace UMA.AssetBundles
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal class ReadOnlyAttribute : PropertyAttribute
#else
	public class ReadOnlyAttribute : PropertyAttribute
#endif
	{

	}
}
