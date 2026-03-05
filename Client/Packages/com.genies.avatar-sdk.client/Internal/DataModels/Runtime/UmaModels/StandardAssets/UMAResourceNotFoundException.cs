using System;

namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal class UMAResourceNotFoundException : Exception
#else
	public class UMAResourceNotFoundException : Exception
#endif
	{
		public UMAResourceNotFoundException(string message)
			: base(message)
		{
		}
	}
}
