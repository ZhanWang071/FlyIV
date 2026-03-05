using System;
using System.Collections.Generic;
using GnWrappers;
using Unity.Collections;
using UnityEngine;

using Pose = GnWrappers.Pose;

namespace Genies.Naf
{
    /**
     * Helper class that synchronizes a native blend shape pose with a Unity skeleton.
     */
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NativeBlendShapePoseSynchronizer : IDisposable
#else
    public sealed class NativeBlendShapePoseSynchronizer : IDisposable
#endif
    {
        private readonly PoseContext _context;
        private readonly Pose        _pose;
        private readonly PoseMask    _dirtyMask;

        private readonly List<RendererData> _renderers;
        private readonly NativeArray<float> _poseWeights;

        public NativeBlendShapePoseSynchronizer(PoseContext context, Pose pose, IEnumerable<SkinnedMeshRenderer> renderers, PoseMask dirtyMask = null)
        {
            if (context.IsNull() || pose.IsNull() || renderers is null)
            {
                throw new ArgumentException("Invalid argument(s) provided to NativeBlendShapePoseSynchronizer constructor.");
            }

            _context   = context;
            _pose      = pose;
            _dirtyMask = dirtyMask;

            _renderers = new List<RendererData>();

            // gather the float channel names from the definition
            using PoseDefinition definition = _context.Definition();
            int floatCount = definition.GetFloatCount();
            List<string> poseShapeNames = new List<string>(floatCount);
            for (int i = 0; i < floatCount; ++i)
            {
                poseShapeNames.Add(definition.GetFloatName(i));
            }

            // initialize renderer data
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                var rendererData = new RendererData(renderer);
                _renderers.Add(rendererData);

                for (int i = 0; i < poseShapeNames.Count; ++i)
                {
                    if (TryFindShapeIndex(poseShapeNames[i], mesh, out int shapeIndex))
                    {
                        rendererData.BlendShapes.Add((i, shapeIndex));
                    }
                }
            }

            // get the pose transforms as a native array to improve performance
            using DynamicAccessor accessor = _pose.GetFloatAccessor();
            _poseWeights = accessor.AsNativeArray<float>();

            // apply the rest pose initially
            ApplyRestPose();
        }

        public void Sync()
        {
            bool allDirty = true;
            if (_dirtyMask is not null && !_dirtyMask.IsNull())
            {
                PoseMaskFilter filter = _dirtyMask.FloatFilter();
                if (filter == PoseMaskFilter.FilterNone)
                {
                    return;
                }

                allDirty = filter == PoseMaskFilter.FilterAll;
            }

            if (allDirty)
            {
                foreach (RendererData rendererData in _renderers)
                {
                    SkinnedMeshRenderer renderer = rendererData.Renderer;
                    if (renderer.sharedMesh != rendererData.Mesh)
                    {
                        continue;
                    }

                    foreach (var (poseIndex, shapeIndex) in rendererData.BlendShapes)
                    {
                        renderer.SetBlendShapeWeight(shapeIndex, _poseWeights[poseIndex] * 100f);
                    }
                }
                return;
            }

            foreach (RendererData rendererData in _renderers)
            {
                SkinnedMeshRenderer renderer = rendererData.Renderer;
                if (renderer.sharedMesh != rendererData.Mesh)
                {
                    continue;
                }

                foreach (var (poseIndex, shapeIndex) in rendererData.BlendShapes)
                {
                    if (_dirtyMask.GetFloatTrack(poseIndex))
                    {
                        renderer.SetBlendShapeWeight(shapeIndex, _poseWeights[poseIndex] * 100f);
                    }
                }
            }
        }

        public void ApplyRestPose()
        {
            using Pose               restPose    = _context.RestPose();
            using DynamicAccessor    accessor    = restPose.GetFloatAccessor();
            using NativeArray<float> poseWeights = accessor.AsNativeArray<float>();

            foreach (RendererData rendererData in _renderers)
            {
                SkinnedMeshRenderer renderer = rendererData.Renderer;
                if (renderer.sharedMesh != rendererData.Mesh)
                {
                    continue;
                }

                foreach (var (poseIndex, shapeIndex) in rendererData.BlendShapes)
                {
                    renderer.SetBlendShapeWeight(shapeIndex, poseWeights[poseIndex] * 100f);
                }
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _pose.Dispose();
            _dirtyMask?.Dispose();
            _poseWeights.Dispose();
        }

        private static bool TryFindShapeIndex(string name, Mesh mesh, out int index)
        {
            index = -1;
            for (int i = 0; i < mesh.blendShapeCount; ++i)
            {
                if (mesh.GetBlendShapeName(i).Contains(name))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private sealed class RendererData
        {
            public readonly SkinnedMeshRenderer                   Renderer;
            public readonly Mesh                                  Mesh;
            public readonly List<(int poseIndex, int shapeIndex)> BlendShapes;

            public RendererData(SkinnedMeshRenderer renderer)
            {
                Renderer    = renderer;
                Mesh        = renderer.sharedMesh;
                BlendShapes = new List<(int poseIndex, int shapeIndex)>();
            }
        }
    }
}
