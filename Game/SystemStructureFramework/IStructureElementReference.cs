namespace Snm.Framework.System
{
    public interface IStructureElementReference
    {
        string InjectId { get; }
        IStructureElementDefinition ReferenceAsset { get; }
    }
}