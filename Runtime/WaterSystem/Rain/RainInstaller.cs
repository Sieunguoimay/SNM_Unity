using Snm.DependencyInjection;

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
                    return Install(ctx);
                });
        }

        public static RainFeature Install(WaterFeatureContext ctx)
        {
            return new RainFeature(
                ctx.SurfaceMaterial,
                ctx.Config.rain);
        }
    }
}
