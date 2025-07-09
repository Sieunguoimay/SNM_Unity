namespace Snm.Framework.System
{
    public interface IDepedencyResolver
    {
        T Resolve<T>();
    }
}