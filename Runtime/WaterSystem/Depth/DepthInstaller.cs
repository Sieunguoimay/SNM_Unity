using Snm.DependencyInjection;

namespace Snm.WaterSystem.Depth
{
    public static class DepthInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<DepthFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static DepthFeature Install(WaterFeatureContext ctx)
        {
            return new DepthFeature(
                ctx.SurfaceMaterial,
                ctx.Config.depth);
        }
    }
}
