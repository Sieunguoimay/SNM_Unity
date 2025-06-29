using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.SystemStructureFramework
{

    public class SystemStructure : IDisposable
    {
        private readonly Dictionary<IStructureElementDefinition, IStructureElement> unitRegistry;
        private readonly IEnumerable<IStructureElementLifecycle> lifecycles;

        public Dictionary<IStructureElementDefinition, IStructureElement> UnitRegistry => unitRegistry;

        public SystemStructure(Dictionary<IStructureElementDefinition, IStructureElement> unitRegistry)
        {
            this.unitRegistry = unitRegistry;
            lifecycles = this.unitRegistry.Values.OfType<IStructureElementLifecycle>();

            foreach (var unit in lifecycles)
            {
                unit.Initialize();
                Debug.Log("Initialize of " + unit.GetType().Name + " completed.");
            }

            foreach (var unit in lifecycles)
            {
                unit.Setup();
                Debug.Log("Setup of " + unit.GetType().Name + " completed.");
            }

            Debug.Log("Created LifecycleStructure. Structure contains " + this.unitRegistry.Count + " lifecycle units.");
        }

        public void Dispose()
        {
            foreach (var unit in lifecycles)
            {
                unit.Teardown();
                Debug.Log("Teardown of " + unit.GetType().Name + " completed.");
            }

            foreach (var unit in lifecycles)
            {
                unit.Cleanup();
                Debug.Log("Cleanup of " + unit.GetType().Name + " completed.");
            }

            unitRegistry.Clear();
            Debug.Log("Disposed LifecycleStructure.");
        }
    }
}