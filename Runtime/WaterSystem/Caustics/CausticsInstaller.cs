using Snm.DependencyInjection;

namespace Snm.WaterSystem.Caustics
{
    public static class CausticsInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<CausticsHandle>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static CausticsHandle Install(WaterFeatureContext ctx)
        {
            var feature = new CausticsFeature(
                ctx.SurfaceMaterial,
                ctx.Config.caustics,
                ctx.UpdateService);

            return new CausticsHandle(feature);
        }
    }
}
