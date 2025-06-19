using System.Collections.Generic;

namespace Snm.LifecycleStructureFramework
{
    public interface ILifecycleUnitDefinition
    {
        ILifecycleUnit CreateLifecycleUnit();
        IReadOnlyList<ILifecycleUnitReference> UnitReferences { get; }
    }
}