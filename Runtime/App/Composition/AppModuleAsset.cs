using Snm.DependencyInjection;
using UnityEngine;

namespace Snm.Runtime.App.Composition
{
    public abstract class AppModuleAsset : ScriptableObject, IAppModule
    {
        public abstract void Configure(IBindingContext context);
    }
}
