namespace SNM.Lifecycle
{
    /// <summary>
    /// You want to reference to other objects safely without worrying about
    /// the order of Destroy() when Unloading the game.
    /// This Lifecycle system is for you!
    /// </summary>
    public interface ILifecycle
    {
        void Initialize();//get system ready: spawn internal objects
        void AfterInitialize();//connect to sibling systems
        void BeforeDispose();//disconnect from sibling systems
        void Dispose();//destroy spawned stuffs
    }
}
