namespace SNM.Lifecycle
{
    /// <summary>
    /// This is the interface to the ILifecycleManager
    /// You want to reference to other objects safely without worrying about
    /// the order of Destroy() when Unloading the game.
    /// This Lifecycle system is for you!
    /// </summary>
    public interface ILifecycle
    {
        void Initialize(ILifecycleManager manager);//get system ready: spawn internal objects
        void Dispose();//destroy spawned stuffs
    }

    public interface IBatchLifecycle : ILifecycle
    {
        void AfterInitialize();//connect to sibling systems
        void BeforeDispose();//disconnect from sibling systems
    }

    /// <summary>
    /// Use this interface if you want to destroy the object
    /// </summary>
    public interface IAutoDisposeLifecycle
    {
        void RequestDispose();
    }
}
