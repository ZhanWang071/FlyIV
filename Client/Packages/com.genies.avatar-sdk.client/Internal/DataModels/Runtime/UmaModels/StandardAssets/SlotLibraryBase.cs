using System;
using System.Collections.Generic;
using UnityEngine;

namespace UMA
{
	/// <summary>
	/// Base class for UMA slot libraries.
	/// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
	[AddComponentMenu("")]
	internal abstract class SlotLibraryBase : MonoBehaviour 
#else
	public abstract class SlotLibraryBase : MonoBehaviour 
#endif
	{
		public virtual void AddSlotAsset(SlotDataAsset slot) { throw new NotFiniteNumberException(); }
		public virtual SlotDataAsset[] GetAllSlotAssets() { throw new NotFiniteNumberException(); }
		public abstract SlotData InstantiateSlot(string name);
		public abstract SlotData InstantiateSlot(int nameHash);
		public abstract SlotData InstantiateSlot(string name, List<OverlayData> overlayList);
		public abstract SlotData InstantiateSlot(int nameHash, List<OverlayData> overlayList);
		public virtual bool HasSlot(string name) { throw new NotImplementedException(); }
		public virtual bool HasSlot(int nameHash) { throw new NotImplementedException(); }

		public abstract void UpdateDictionary();
		public abstract void ValidateDictionary();
	}
}
