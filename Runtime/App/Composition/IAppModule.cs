using Snm.DependencyInjection;

namespace Snm.Runtime.App.Composition
{
    public interface IAppModule
    {
        void Configure(IBindingContext context);
    }
}
