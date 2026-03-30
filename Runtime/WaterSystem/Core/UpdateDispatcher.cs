using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.Unity
{
    public enum UpdatePriority
    {
        Input       = 0,
        PrePhysics  = 100,
        Physics     = 200,
        PostPhysics = 300,
        Camera      = 400,
        Late        = 500,
    }

    public interface ILateUpdateTarget
    {
        void LateUpdate();
    }
    public interface IUpdateTarget
    {
        void Update(float deltaTime);
    }
    public interface IFixedUpdateTarget
    {
        void FixedUpdate(float fixedDeltaTime);
    }

    public interface IUpdateService
    {
        void AddUpdateTarget(IUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics);
        void AddLateUpdateTarget(ILateUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics);
        void AddFixedUpdateTarget(IFixedUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics);
        void RemoveUpdateTarget(IUpdateTarget target);
        void RemoveLateUpdateTarget(ILateUpdateTarget target);
        void RemoveFixedUpdateTarget(IFixedUpdateTarget target);
    }

    [ExecuteInEditMode]
    public class UpdateDispatcher : MonoBehaviour, IUpdateService
    {
        private readonly List<(IUpdateTarget target, UpdatePriority priority)> targets = new();
        private readonly List<(ILateUpdateTarget target, UpdatePriority priority)> lateUpdateTargets = new();
        private readonly List<(IFixedUpdateTarget target, UpdatePriority priority)> fixedUpdateTargets = new();

        private IUpdateTarget[] _updateSnapshot = Array.Empty<IUpdateTarget>();
        private ILateUpdateTarget[] _lateSnapshot = Array.Empty<ILateUpdateTarget>();
        private IFixedUpdateTarget[] _fixedSnapshot = Array.Empty<IFixedUpdateTarget>();
        private bool _updateDirty = true;
        private bool _lateDirty = true;
        private bool _fixedDirty = true;

        public void AddUpdateTarget(IUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics)
        {
            targets.Add((target, priority));
            _updateDirty = true;
        }

        public void AddLateUpdateTarget(ILateUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics)
        {
            lateUpdateTargets.Add((target, priority));
            _lateDirty = true;
        }

        public void AddFixedUpdateTarget(IFixedUpdateTarget target, UpdatePriority priority = UpdatePriority.Physics)
        {
            fixedUpdateTargets.Add((target, priority));
            _fixedDirty = true;
        }

        public void RemoveUpdateTarget(IUpdateTarget target)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i].target == target)
                {
                    targets.RemoveAt(i);
                    _updateDirty = true;
                    break;
                }
            }
        }

        public void RemoveLateUpdateTarget(ILateUpdateTarget target)
        {
            for (int i = lateUpdateTargets.Count - 1; i >= 0; i--)
            {
                if (lateUpdateTargets[i].target == target)
                {
                    lateUpdateTargets.RemoveAt(i);
                    _lateDirty = true;
                    break;
                }
            }
        }

        public void RemoveFixedUpdateTarget(IFixedUpdateTarget target)
        {
            for (int i = fixedUpdateTargets.Count - 1; i >= 0; i--)
            {
                if (fixedUpdateTargets[i].target == target)
                {
                    fixedUpdateTargets.RemoveAt(i);
                    _fixedDirty = true;
                    break;
                }
            }
        }

        private void Update()
        {
            if (_updateDirty) RebuildUpdateSnapshot();

            float deltaTime = Time.deltaTime;
            foreach (var t in _updateSnapshot)
                t.Update(deltaTime);
        }

        private void FixedUpdate()
        {
            if (_fixedDirty) RebuildFixedSnapshot();

            float fixedDeltaTime = Time.fixedDeltaTime;
            foreach (var t in _fixedSnapshot)
                t.FixedUpdate(fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (_lateDirty) RebuildLateSnapshot();

            foreach (var t in _lateSnapshot)
                t.LateUpdate();
        }

        private void RebuildUpdateSnapshot()
        {
            targets.Sort((a, b) => a.priority.CompareTo(b.priority));
            _updateSnapshot = new IUpdateTarget[targets.Count];
            for (int i = 0; i < targets.Count; i++)
                _updateSnapshot[i] = targets[i].target;
            _updateDirty = false;
        }

        private void RebuildFixedSnapshot()
        {
            fixedUpdateTargets.Sort((a, b) => a.priority.CompareTo(b.priority));
            _fixedSnapshot = new IFixedUpdateTarget[fixedUpdateTargets.Count];
            for (int i = 0; i < fixedUpdateTargets.Count; i++)
                _fixedSnapshot[i] = fixedUpdateTargets[i].target;
            _fixedDirty = false;
        }

        private void RebuildLateSnapshot()
        {
            lateUpdateTargets.Sort((a, b) => a.priority.CompareTo(b.priority));
            _lateSnapshot = new ILateUpdateTarget[lateUpdateTargets.Count];
            for (int i = 0; i < lateUpdateTargets.Count; i++)
                _lateSnapshot[i] = lateUpdateTargets[i].target;
            _lateDirty = false;
        }
    }
}
