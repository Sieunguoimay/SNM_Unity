using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.Unity
{
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
        void AddUpdateTarget(IUpdateTarget target);
        void AddLateUpdateTarget(ILateUpdateTarget target);
        void AddFixedUpdateTarget(IFixedUpdateTarget target);
        void RemoveUpdateTarget(IUpdateTarget target);
        void RemoveLateUpdateTarget(ILateUpdateTarget target);
        void RemoveFixedUpdateTarget(IFixedUpdateTarget target);
    }

    [ExecuteInEditMode]
    public class UpdateDispatcher : MonoBehaviour, IUpdateService
    {
        private readonly List<IUpdateTarget> targets = new();
        private readonly List<ILateUpdateTarget> lateUpdateTargets = new();
        private readonly List<IFixedUpdateTarget> fixedUpdateTargets = new();

        // Snapshot arrays for safe iteration while targets add/remove themselves.
        private IUpdateTarget[] _updateSnapshot = System.Array.Empty<IUpdateTarget>();
        private ILateUpdateTarget[] _lateSnapshot = System.Array.Empty<ILateUpdateTarget>();
        private IFixedUpdateTarget[] _fixedSnapshot = System.Array.Empty<IFixedUpdateTarget>();
        private bool _updateDirty = true;
        private bool _lateDirty = true;
        private bool _fixedDirty = true;

        public void AddUpdateTarget(IUpdateTarget target)     { targets.Add(target);            _updateDirty = true; }
        public void AddLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Add(target); _lateDirty = true;   }
        public void AddFixedUpdateTarget(IFixedUpdateTarget target) { fixedUpdateTargets.Add(target); _fixedDirty = true; }
        public void RemoveUpdateTarget(IUpdateTarget target)     { targets.Remove(target);         _updateDirty = true; }
        public void RemoveLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Remove(target); _lateDirty = true; }
        public void RemoveFixedUpdateTarget(IFixedUpdateTarget target) { fixedUpdateTargets.Remove(target); _fixedDirty = true; }

        private void Update()
        {
            if (_updateDirty) { _updateSnapshot = targets.ToArray(); _updateDirty = false; }

            float deltaTime = Time.deltaTime;
            foreach (var t in _updateSnapshot)
            {
                t.Update(deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (_fixedDirty) { _fixedSnapshot = fixedUpdateTargets.ToArray(); _fixedDirty = false; }

            float fixedDeltaTime = Time.fixedDeltaTime;
            foreach (var t in _fixedSnapshot)
            {
                t.FixedUpdate(fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            if (_lateDirty) { _lateSnapshot = lateUpdateTargets.ToArray(); _lateDirty = false; }

            foreach (var t in _lateSnapshot)
            {
                t.LateUpdate();
            }
        }
    }
}