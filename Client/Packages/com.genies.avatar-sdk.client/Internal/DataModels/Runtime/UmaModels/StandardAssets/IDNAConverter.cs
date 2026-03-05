using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
	internal interface IDNAConverter
#else
	public interface IDNAConverter
#endif
	{
		System.Type DNAType { get; }

		string name { get; }

		string DisplayValue { get; }

		int DNATypeHash { get; }

		DNAConvertDelegate PreApplyDnaAction { get; }

		DNAConvertDelegate ApplyDnaAction { get; }

		void Prepare();

	}
}