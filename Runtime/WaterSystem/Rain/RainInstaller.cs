using Snm.DependencyInjection;
using Snm.WaterSystem.Wave;

namespace Snm.WaterSystem.Rain
{
    public static class RainInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<RainFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    var waveSim = r.Resolve<IWaveSimulation>();
                    return new RainFeature(waveSim, ctx.Config.rain);
                });
        }
    }
}
