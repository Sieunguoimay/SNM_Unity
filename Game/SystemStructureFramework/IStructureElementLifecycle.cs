namespace Snm.Framework.System
{
    public interface IStructureElementLifecycle : IStructureElement
    {
        void Initialize();
        void Setup();
        void Teardown();
        void Cleanup();
    }
}