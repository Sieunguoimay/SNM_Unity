namespace FruitCollectorGame
{
    public interface ILifecycle
    {
        void SetupInternal();//get system ready: spawn internal objects
        void SetupDependencies();//connect to sibling systems
        void TearDownDependencies();//disconnect from sibling systems
        void DestroyInternal();//destroy spawned stuffs
    }
}
