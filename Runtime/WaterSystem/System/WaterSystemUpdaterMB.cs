using System.Collections.Generic;
using UnityEngine;

namespace Snm.Runtime.WaterSystem
{
    public interface ILateUpdateTarget
    {
        void LateUpdate();
    }
    public interface IUpdateTarget
    {
        void Update();
    }

    [ExecuteInEditMode]
    public class WaterSystemUpdaterMB : MonoBehaviour
    {
        private readonly List<IUpdateTarget> targets = new();
        private readonly List<ILateUpdateTarget> lateUpdteTargets = new();

        public void AddUpdateTarget(IUpdateTarget target) { targets.Add(target); }
        public void AddLateUpdateTarget(ILateUpdateTarget target) { lateUpdteTargets.Add(target); }

        private void Update()
        {
            foreach (var t in targets)
            {
                t.Update();
            }
        }

        private void LateUpdate()
        {
            foreach (var t in lateUpdteTargets )
            {
                t.LateUpdate();
            }
        }
    }
}