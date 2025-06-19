namespace Snm.LifecycleStructureFramework
{
    public interface ILifecycleUnitReference
    {
        string InjectId { get; }
        ILifecycleUnitDefinition Asset { get; }
    }
}