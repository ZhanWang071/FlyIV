using System;
using Genies.Utilities;
using UMA;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct UmaMaterialExportMap
#else
    public struct UmaMaterialExportMap
#endif
    {
        [FormerlySerializedAs("mapExporter")] public MaterialMapExporter MapExporter;
        [FormerlySerializedAs("splitTextureSettings")] public SplitTextureSettings SplitTextureSettings;
        [FormerlySerializedAs("umaChannel")] public UMAMaterial.MaterialChannel UmaChannel;
        [FormerlySerializedAs("postProcessingMaterial")] public Material PostProcessingMaterial;
    }
}
