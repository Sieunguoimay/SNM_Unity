using System.Collections.Generic;

namespace Snm.LifecycleStructureFramework
{
    public interface ILifecycleUnitDefinition
    {
        ILifecycleUnit CreateLifecycleUnit(IDepedencyResolver resolver);
        IReadOnlyList<ILifecycleUnitReference> UnitReferences { get; }
    }
}