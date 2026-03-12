using Snm.DependencyInjection;

namespace Snm.WaterSystem.Foam
{
    public static class FoamInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<FoamFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static FoamFeature Install(WaterFeatureContext ctx)
        {
            return new FoamFeature(
                ctx.SurfaceMaterial,
                ctx.Config.foam);
        }
    }
}
