namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal delegate void DNAConvertDelegate(UMAData data, UMASkeleton skeleton);
#else
	public delegate void DNAConvertDelegate(UMAData data, UMASkeleton skeleton);
#endif
}
