using Snm.DependencyInjection;
using Snm.WaterSystem.Wave;

namespace Snm.WaterSystem.Rain
{
    public static class RainInstaller
    {
        public static void Install(IBindingContext container, WaterFeatureContext ctx)
        {
            container.Bind<RainFeature>()
                .ToFactory(r =>
                {
                    var waveSim = r.Resolve<IWaveSimulation>();
                    return new RainFeature(waveSim, ctx.Config.rain);
                }).AsScoped();
        }
    }
}
