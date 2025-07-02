namespace Snm.SystemStructureFramework
{
    public interface IDepedencyResolver
    {
        T Resolve<T>();
    }
}