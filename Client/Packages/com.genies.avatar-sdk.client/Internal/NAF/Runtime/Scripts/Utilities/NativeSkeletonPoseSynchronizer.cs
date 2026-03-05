using System;
using System.Collections.Generic;
using GnWrappers;
using Unity.Collections;
using UnityEngine;

using Pose = GnWrappers.Pose;

namespace Genies.Naf
{
    /**
     * Helper class that synchronizes a native skeleton pose with a Unity skeleton.
     */
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NativeSkeletonPoseSynchronizer : IDisposable
#else
    public sealed class NativeSkeletonPoseSynchronizer : IDisposable
#endif
    {
        private readonly PoseContext _context;
        private readonly Pose        _pose;
        private readonly PoseMask    _dirtyMask;

        private readonly List<Transform>           _bones;
        private readonly List<int>                 _indices;
        private readonly NativeArray<OzzTransform> _poseTransforms;
        private readonly NativeArray<OzzTransform> _restPoseTransforms;

        public NativeSkeletonPoseSynchronizer(PoseContext context, Pose pose, Transform skeletonRoot, PoseMask dirtyMask = null)
        {
            if (context.IsNull() || pose.IsNull() || !skeletonRoot)
            {
                throw new ArgumentException("Invalid argument(s) provided to NativeSkeletonPoseSynchronizer constructor.");
            }

            _context   = context;
            _pose      = pose;
            _dirtyMask = dirtyMask;

            using PoseDefinition definition = _context.Definition();
            _bones   = new List<Transform>(definition.GetSkeletonCount());
            _indices = new List<int>(definition.GetSkeletonCount());

            // get the pose transforms as a native array to improve performance
            _pose.ReadSkeletonLocals();
            using DynamicAccessor accessor = _pose.GetSkeletonLocals();
            _poseTransforms = accessor.AsNativeArray<OzzTransform>();

            AddBone(skeletonRoot, definition);

            // apply the rest pose initially
            ApplyRestPose();
        }

        public void Sync()
        {
            _pose.ReadSkeletonLocals();

            bool allDirty = true;
            if (_dirtyMask is not null && !_dirtyMask.IsNull())
            {
                PoseMaskFilter filter = _dirtyMask.SkeletonFilter();
                if (filter == PoseMaskFilter.FilterNone)
                {
                    return;
                }

                allDirty = filter == PoseMaskFilter.FilterAll;
            }

            // if all tracks are dirty, we can skip the per-track dirty check
            if (allDirty)
            {
                for (int i = 0; i < _bones.Count; ++i)
                {
                    OzzTransform transform = _poseTransforms[_indices[i]];
                    Transform    bone      = _bones[i];

                    bone.localPosition = transform.Translation;
                    bone.localRotation = transform.Rotation;
                    bone.localScale    = transform.Scale;
                }
                return;
            }

            // otherwise, only update the dirty tracks
            for (int i = 0; i < _bones.Count; ++i)
            {
                int poseIndex = _indices[i];
                if (!_dirtyMask.GetSkeletonTrack(poseIndex))
                {
                    continue;
                }

                OzzTransform transform = _poseTransforms[poseIndex];
                Transform    bone      = _bones[i];

                bone.localPosition = transform.Translation;
                bone.localRotation = transform.Rotation;
                bone.localScale    = transform.Scale;
            }
        }

        public void ApplyRestPose()
        {
            using Pose restPose = _context.RestPose();
            restPose.ReadSkeletonLocals();

            using DynamicAccessor accessor = _pose.GetSkeletonLocals();
            using NativeArray<OzzTransform> transforms = accessor.AsNativeArray<OzzTransform>();

            for (int i = 0; i < _bones.Count; ++i)
            {
                OzzTransform transform = transforms[_indices[i]];
                Transform    bone      = _bones[i];

                bone.localPosition = transform.Translation;
                bone.localRotation = transform.Rotation;
                bone.localScale    = transform.Scale;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _pose.Dispose();
            _dirtyMask?.Dispose();
            _poseTransforms.Dispose();
        }

        private void AddBone(Transform bone, PoseDefinition definition)
        {
            for (int i = 0; i < bone.childCount; ++i)
            {
                AddBone(bone.GetChild(i), definition);
            }

            int index = definition.FindSkeletonIndex(bone.name);
            if (index < 0)
            {
                return;
            }

            _bones.Add(bone);
            _indices.Add(index);
        }
    }
}
