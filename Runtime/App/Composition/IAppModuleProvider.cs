namespace Snm.App.Composition
{
    public interface IAppModuleProvider
    {
        IAppModule[] GetModules();
    }
}
