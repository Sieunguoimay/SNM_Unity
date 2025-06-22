using System;
using System.Collections.Generic;
using UnityEngine;

namespace Snm.LifecycleStructureFramework
{

    public class LifecycleStructure : IDisposable
    {
        private readonly Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> unitRegistry;

        public Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> UnitRegistry => unitRegistry;

        public LifecycleStructure(Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> unitRegistry)
        {
            this.unitRegistry = unitRegistry;

            foreach (var unit in unitRegistry.Values)
            {
                unit.Initialize();
                Debug.Log("Initialize of " + unit.GetType().Name + " completed.");
            }

            foreach (var unit in unitRegistry.Values)
            {
                unit.Setup();
                Debug.Log("Setup of " + unit.GetType().Name + " completed.");
            }

            Debug.Log("Created LifecycleStructure. Structure contains " + unitRegistry.Count + " lifecycle units.");
        }

        public void Dispose()
        {
            foreach (var unit in unitRegistry.Values)
            {
                unit.Teardown();
                Debug.Log("Teardown of " + unit.GetType().Name + " completed.");
            }

            foreach (var unit in unitRegistry.Values)
            {
                unit.Cleanup();
                Debug.Log("Cleanup of " + unit.GetType().Name + " completed.");
            }

            unitRegistry.Clear();
            Debug.Log("Disposed LifecycleStructure.");
        }
    }
}