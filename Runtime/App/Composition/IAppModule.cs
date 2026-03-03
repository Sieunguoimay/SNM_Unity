using Snm.App.DependencyInjection;

namespace Snm.App.Composition
{
    public interface IAppModule
    {
        void Configure(IBindingContext context);
    }
}
