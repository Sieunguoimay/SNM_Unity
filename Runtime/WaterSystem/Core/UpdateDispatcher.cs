using System.Collections.Generic;
using UnityEngine;

namespace Snm.WaterSystem
{
    public interface ILateUpdateTarget
    {
        void LateUpdate();
    }
    public interface IUpdateTarget
    {
        void Update(float deltaTime);
    }

    public interface IUpdateService
    {
        void AddUpdateTarget(IUpdateTarget target);
        void AddLateUpdateTarget(ILateUpdateTarget target);
        void RemoveUpdateTarget(IUpdateTarget target);
        void RemoveLateUpdateTarget(ILateUpdateTarget target);
    }

    [ExecuteInEditMode]
    public class UpdateDispatcher : MonoBehaviour, IUpdateService
    {
        private readonly List<IUpdateTarget> targets = new();
        private readonly List<ILateUpdateTarget> lateUpdateTargets = new();

        // Snapshot arrays for safe iteration while targets add/remove themselves.
        private IUpdateTarget[] _updateSnapshot = System.Array.Empty<IUpdateTarget>();
        private ILateUpdateTarget[] _lateSnapshot = System.Array.Empty<ILateUpdateTarget>();
        private bool _updateDirty = true;
        private bool _lateDirty = true;

        public void AddUpdateTarget(IUpdateTarget target)     { targets.Add(target);            _updateDirty = true; }
        public void AddLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Add(target); _lateDirty = true;   }
        public void RemoveUpdateTarget(IUpdateTarget target)     { targets.Remove(target);         _updateDirty = true; }
        public void RemoveLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Remove(target); _lateDirty = true; }

        private void Update()
        {
            if (_updateDirty) { _updateSnapshot = targets.ToArray(); _updateDirty = false; }

            float deltaTime = Time.deltaTime;
            foreach (var t in _updateSnapshot)
            {
                t.Update(deltaTime);
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