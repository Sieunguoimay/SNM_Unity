namespace Snm.SystemStructureFramework
{
    public interface IStructureElementReference
    {
        string InjectId { get; }
        IStructureElementDefinition ReferenceAsset { get; }
    }
}