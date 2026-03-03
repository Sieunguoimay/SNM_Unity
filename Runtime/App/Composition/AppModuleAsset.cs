using Snm.App.DependencyInjection;
using UnityEngine;

namespace Snm.App.Composition
{
    public abstract class AppModuleAsset : ScriptableObject, IAppModule
    {
        public abstract void Configure(IBindingContext context);
    }
}
