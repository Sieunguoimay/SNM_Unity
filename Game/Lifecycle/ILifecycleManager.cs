namespace SNM.Lifecycle
{
    /// <summary>
    /// Any lifecycle object has its LifecycleManager which sets it up
    /// </summary>
    public interface ILifecycleManager
    {
    }

    /// <summary>
    /// Meant to setup single lifecycle at once
    /// </summary>
    public interface IDynamicLifecycleManager : ILifecycleManager
    {
        void Initialize(ILifecycle lifecycle);
        void Dispose(ILifecycle lifecycle);
    }
}
