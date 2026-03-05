using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UtilityVector
#else
    public sealed class UtilityVector
#endif
    {
        public readonly string Name;
        public readonly string Version;
        public readonly IReadOnlyList<UtilMesh> UtilMeshes;

        public UtilityVector(string name, string version, IReadOnlyList<UtilMesh> utilMeshes)
        {
            Name = name;
            Version = version;
            UtilMeshes = utilMeshes;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UtilMesh
#else
    public sealed class UtilMesh
#endif
    {
        public readonly UtilMeshName Name;
        public readonly IReadOnlyList<UtilMeshRegion> Regions;

        public UtilMesh(UtilMeshName name, IReadOnlyList<UtilMeshRegion> regions)
        {
            Name = name;
            Regions = regions;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UtilMeshRegion
#else
    public sealed class UtilMeshRegion
#endif
    {
        public readonly RegionType Region;
        public readonly Vector3[] UniquePoints;

        public UtilMeshRegion(RegionType region, Vector3[] uniquePoints)
        {
            Region = region;
            UniquePoints = uniquePoints;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum UtilMeshName
#else
    public enum UtilMeshName
#endif
    {
        bodysuit,
        dress,
        outerwear,
        pants,
        scalp,
        shirt,
        shoes,
        skirt,
        none
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum RegionType
#else
    public enum RegionType
#endif
    {
        wholeTarget,
        biceps,
        calves,
        chest,
        forearms,
        hands,
        head,
        hips,
        neck,
        shoulders,
        thighs,
        waist
    }
}
