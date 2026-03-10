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

        public void AddUpdateTarget(IUpdateTarget target) { targets.Add(target); }
        public void AddLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Add(target); }
        public void RemoveUpdateTarget(IUpdateTarget target) { targets.Remove(target); }
        public void RemoveLateUpdateTarget(ILateUpdateTarget target) { lateUpdateTargets.Remove(target); }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var t in targets)
            {
                t.Update(deltaTime);
            }
        }

        private void LateUpdate()
        {
            foreach (var t in lateUpdateTargets)
            {
                t.LateUpdate();
            }
        }
    }
}