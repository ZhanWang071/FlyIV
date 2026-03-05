namespace UMA
{
	/// <summary>
	/// Base class for texture processing coroutines.
	/// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
	internal abstract class TextureProcessBaseCoroutine : WorkerCoroutine
#else
	public abstract class TextureProcessBaseCoroutine : WorkerCoroutine
#endif
	{
	    public abstract void Prepare(UMAData _umaData, UMAGeneratorBase _umaGenerator);
	}
}