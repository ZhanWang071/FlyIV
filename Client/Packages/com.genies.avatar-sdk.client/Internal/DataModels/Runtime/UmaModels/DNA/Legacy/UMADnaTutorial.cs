using System.Collections;
using UnityEngine;

namespace UMA
{
	[System.Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal partial class UMADnaTutorial : UMADna
#else
	public partial class UMADnaTutorial : UMADna
#endif
	{
		public float eyeSpacing = 0.5f;
	}
}