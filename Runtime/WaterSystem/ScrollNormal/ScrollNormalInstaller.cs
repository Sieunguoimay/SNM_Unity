using Snm.DependencyInjection;

namespace Snm.WaterSystem.ScrollNormal
{
    public static class ScrollNormalInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<ScrollNormalFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static ScrollNormalFeature Install(WaterFeatureContext ctx)
        {
            return new ScrollNormalFeature(
                ctx.SurfaceMaterial,
                ctx.Config.scrollNormal);
        }
    }
}
