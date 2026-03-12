using System.Collections.Generic;
using Snm.DependencyInjection;

namespace Snm.WaterSystem.Wave
{
    public static class WaveDisturberInstaller
    {
        /// <param name="container">The DI binding context.</param>
        /// <param name="disturbers">
        ///   Live enumerable of disturbers (e.g. from EnvironmentInteractionSystem.Interactors).
        ///   The reference is held and re-enumerated every frame, so the list stays in sync.
        /// </param>
        public static void Install(IBindingContext container, IEnumerable<IWaveDisturber> disturbers)
        {
            container.Bind<WaveDisturberFeature>()
                .ToScoped(r =>
                {
                    var ctx     = r.Resolve<WaterFeatureContext>();
                    var waveSim = r.Resolve<IWaveSimulation>();
                    var tracker = new WaveDisturberTracker(
                        disturbers,
                        ctx.Surface,
                        waveSim,
                        ctx.Config.disturber);
                    return new WaveDisturberFeature(tracker);
                });
        }
    }
}
