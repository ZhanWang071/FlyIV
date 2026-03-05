using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class DeformDriverData
#else
    public sealed class DeformDriverData
#endif
    {
        public Vector3[] uniquePoints;

        [JsonProperty]
        private float[,] _serializableWeightMatrix;

        [JsonIgnore]
        private Matrix<float> _weightMatrix;

        [JsonIgnore]
        public Matrix<float> WeightMatrix
        {
            get
            {
                if (_weightMatrix == null && _serializableWeightMatrix != null)
                {
                    _weightMatrix = Matrix<float>.Build.DenseOfArray(_serializableWeightMatrix);
                }

                return _weightMatrix;
            }
            set
            {
                _serializableWeightMatrix = value.ToArray();
                _weightMatrix = value;
            }
        }

        [JsonConstructor]
        private DeformDriverData() { }

        public DeformDriverData(in Vector3[] points)
        {
            uniquePoints = points;
        }
    }
}