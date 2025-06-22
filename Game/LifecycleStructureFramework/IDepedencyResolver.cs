namespace Snm.LifecycleStructureFramework
{
    public interface IDepedencyResolver
    {
        T Resolve<T>();
    }
}