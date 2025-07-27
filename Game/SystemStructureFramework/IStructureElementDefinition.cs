using System.Collections.Generic;

namespace Snm.Framework.System
{
    public interface IStructureElementDefinition
    {
        IStructureElement CreateLifecycleUnit(IDepedencyResolver resolver);
        IReadOnlyList<IStructureElementReference> ElementReferences { get; }
    }
}