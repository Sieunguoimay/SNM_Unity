using System.Collections.Generic;

namespace Snm.Framework.System
{
    public interface IStructureElementDefinition
    {
        IStructureElement CreateElement(IDepedencyResolver resolver);
        IReadOnlyList<IStructureElementReference> ElementReferences { get; }
    }
}