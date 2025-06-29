namespace Snm.SystemStructureFramework
{
    public interface IStructureElement
    {

    }

    public interface IStructureElementLifecycle : IStructureElement
    {
        void Initialize();
        void Setup();
        void Teardown();
        void Cleanup();
    }
}