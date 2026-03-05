using System;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.Events;

namespace UMA
{
	/// <summary>
	/// UMA event occuring on UMA data.
	/// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UMADataEvent : UnityEvent<UMAData>
#else
    public class UMADataEvent : UnityEvent<UMAData>
#endif
    {
        public UMADataEvent()
        {
        }

        public UMADataEvent(UMADataEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(UMAData) }));
			}
		}
		public void AddAction(Action<UMAData> action)
		{
			this.AddListener(action.Target, action.Method);
		}
		public void RemoveAction(Action<UMAData> action)
		{
			this.RemoveListener(action.Target, action.Method);
		}
	}

	/// <summary>
	/// UMA event occuring on slot.
	/// </summary>
	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UMADataSlotEvent : UnityEvent<UMAData, SlotData>
#else
    public class UMADataSlotEvent : UnityEvent<UMAData, SlotData>
#endif
    {
        public UMADataSlotEvent()
        {
        }
		public UMADataSlotEvent(UMADataSlotEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(UMAData), typeof(SlotData) }));
			}
		}
    }

	/// <summary>
	/// UMA event occuring on material.
	/// </summary>
	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UMADataSlotMaterialRectEvent : UnityEvent<UMAData, SlotData, Material, Rect>
#else
    public class UMADataSlotMaterialRectEvent : UnityEvent<UMAData, SlotData, Material, Rect>
#endif
    {
        public UMADataSlotMaterialRectEvent()
        {
        }
		public UMADataSlotMaterialRectEvent(UMADataSlotMaterialRectEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(UMAData), typeof(SlotData), typeof(Material), typeof(Rect) }));
			}
		}
    }

	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMADataWardrobeEvent : UnityEvent<UMAData, UMAWardrobeRecipe>
#else
	public class UMADataWardrobeEvent : UnityEvent<UMAData, UMAWardrobeRecipe>
#endif
	{
		public UMADataWardrobeEvent()
		{
		}
		public UMADataWardrobeEvent(UMADataWardrobeEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(UMAData), typeof(UMAWardrobeRecipe) }));
			}
		}
	}

	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMAExpressionEvent: UnityEvent<UMAData, string, float>
#else
	public class UMAExpressionEvent: UnityEvent<UMAData, string, float>
#endif
    {
		public UMAExpressionEvent()
		{
		}
		public UMAExpressionEvent(UMAExpressionEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(UMAData), typeof(string), typeof(float) }));
			}
		}
	}

	[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMARandomAvatarEvent: UnityEvent<GameObject, GameObject>
#else
	public class UMARandomAvatarEvent: UnityEvent<GameObject, GameObject>
#endif
    {
		public UMARandomAvatarEvent()
		{
		}
		public UMARandomAvatarEvent(UMARandomAvatarEvent source)
		{
			for (int i = 0; i < source.GetPersistentEventCount(); i++)
			{
				var target = source.GetPersistentTarget(i);
				AddListener(target, UnityEventBase.GetValidMethodInfo(target, source.GetPersistentMethodName(i), new Type[] { typeof(GameObject), typeof(GameObject) }));
			}
		}
	}
}
