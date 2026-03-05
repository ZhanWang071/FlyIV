using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMA
{
	//IDNAConverters dont need to have a DynamicUMADnaAsset (their names are hard coded)
	//IDynamicDNAConverters do have dna assets.
	//we could get rid of this shit if we finally ditched the legacy hard coded converters
#if GENIES_SDK && !GENIES_INTERNAL
	internal interface IDynamicDNAConverter
#else
	public interface IDynamicDNAConverter
#endif
	{
		DynamicUMADnaAsset dnaAsset { get; }
	}
}
