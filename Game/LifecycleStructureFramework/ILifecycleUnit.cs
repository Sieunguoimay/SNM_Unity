namespace Snm.LifecycleStructureFramework
{
    public interface ILifecycleUnit
    {
        void Initialize();
        void Setup();
        void Teardown();
        void Cleanup();
    }
}