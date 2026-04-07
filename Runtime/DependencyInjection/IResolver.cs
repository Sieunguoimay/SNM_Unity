namespace Snm.DependencyInjection
{
    public interface IResolver
    {
        TType Resolve<TType>(string id = null) where TType : class;
        TType[] ResolveAll<TType>() where TType : class;
        TType[] ResolveAllLocal<TType>() where TType : class;
    }
}
