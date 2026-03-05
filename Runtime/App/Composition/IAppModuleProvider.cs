namespace Snm.Runtime.App.Composition
{
    public interface IAppModuleProvider
    {
        IAppModule[] GetModules();
    }
}
