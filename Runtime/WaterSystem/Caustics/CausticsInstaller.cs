using Snm.DependencyInjection;

namespace Snm.WaterSystem.Caustics
{
    public static class CausticsInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<CausticsFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static CausticsFeature Install(WaterFeatureContext ctx)
        {
            return new CausticsFeature(
                ctx.SurfaceMaterial,
                ctx.Config.caustics);
        }
    }
}
