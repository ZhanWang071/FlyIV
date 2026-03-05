using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SourceData
#else
    public struct SourceData
#endif
    {
        public Vector3[] uniquePoints;
        public Matrix<float> srcDistanceMatrix;

        public SourceData(in Vector3[] points, in Matrix<float> distmat)
        {
            uniquePoints = points;
            srcDistanceMatrix = distmat;
        }
    }
}