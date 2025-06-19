using System;
using System.Collections.Generic;

namespace Snm.LifecycleStructureFramework
{

    public class LifecycleStructure : IDisposable
    {
        private readonly Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> unitRegistry;

        public Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> UnitRegistry => unitRegistry;

        public LifecycleStructure(Dictionary<ILifecycleUnitDefinition, ILifecycleUnit> unitRegistry)
        {
            this.unitRegistry = unitRegistry;

            foreach (var element in unitRegistry.Values)
            {
                element.Initialize();
            }

            foreach (var element in unitRegistry.Values)
            {
                element.Setup();
            }
        }

        public void Dispose()
        {
            foreach (var element in unitRegistry.Values)
            {
                element.Teardown();
            }

            foreach (var element in unitRegistry.Values)
            {
                element.Cleanup();
            }

            unitRegistry.Clear();
        }
    }
}