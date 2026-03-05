using System;

namespace UMA.CharacterSystem
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal static class EnumExtensions
#else
	public static class EnumExtensions
#endif
	{
		public static bool HasFlagSet(this Enum self, Enum flag)
		{
			if (self.GetType() != flag.GetType())
			{
				throw new ArgumentException("HasFlag : Flag is not of the type of Enum");
			}

			var selfValue = Convert.ToUInt64(self);
			var flagValue = Convert.ToUInt64(flag);

			return (selfValue & flagValue) == flagValue;
		}
	}
}
