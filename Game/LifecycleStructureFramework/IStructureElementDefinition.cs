using System.Collections.Generic;

namespace Snm.SystemStructureFramework
{
    public interface IStructureElementDefinition
    {
        IStructureElement CreateLifecycleUnit(IDepedencyResolver resolver);
        IReadOnlyList<IStructureElementReference> UnitReferences { get; }
    }
}