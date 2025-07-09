using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Snm.SystemStructureFramework
{

    public class SystemStructure : IStructureElementLifecycle, IDisposable
    {
        private readonly Dictionary<IStructureElementDefinition, IStructureElement> elementRegistry;
        private readonly IEnumerable<IStructureElementLifecycle> lifecycles;

        public Dictionary<IStructureElementDefinition, IStructureElement> ElementRegistry => elementRegistry;

        public SystemStructure(Dictionary<IStructureElementDefinition, IStructureElement> elementRegistry)
        {
            this.elementRegistry = elementRegistry;
            lifecycles = this.elementRegistry.Values.OfType<IStructureElementLifecycle>();
            Debug.Log("Created LifecycleStructure. Structure contains " + this.elementRegistry.Count + " lifecycle elements.");
        }

        void IStructureElementLifecycle.Initialize()
        {
            foreach (var element in lifecycles)
            {
                element.Initialize();
                Debug.Log("Initialize of " + element.GetType().Name + " completed.");
            }
        }

        void IStructureElementLifecycle.Setup()
        {
            foreach (var element in lifecycles)
            {
                element.Setup();
                Debug.Log("Setup of " + element.GetType().Name + " completed.");
            }
        }

        void IStructureElementLifecycle.Teardown()
        {
            foreach (var element in lifecycles)
            {
                element.Teardown();
                Debug.Log("Teardown of " + element.GetType().Name + " completed.");
            }
        }

        void IStructureElementLifecycle.Cleanup()
        {
            foreach (var element in lifecycles)
            {
                element.Cleanup();
                Debug.Log("Cleanup of " + element.GetType().Name + " completed.");
            }
        }

        public void Dispose()
        {
            elementRegistry.Clear();
            Debug.Log("Disposed LifecycleStructure.");
        }
    }
}