using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMA.CharacterSystem
{
	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMAPreset
#else
	public class UMAPreset
#endif
	{
		public UMAPredefinedDNA PredefinedDNA;
		public DynamicCharacterAvatar.WardrobeRecipeList DefaultWardrobe;
		public DynamicCharacterAvatar.ColorValueList DefaultColors;
	}
}
