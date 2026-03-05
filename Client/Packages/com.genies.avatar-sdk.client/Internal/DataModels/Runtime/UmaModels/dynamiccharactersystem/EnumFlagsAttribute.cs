using UnityEngine;

namespace UMA.CharacterSystem
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal class EnumFlagsAttribute : PropertyAttribute
#else
	public class EnumFlagsAttribute : PropertyAttribute
#endif
	{
		public EnumFlagsAttribute() { }
	}
}
