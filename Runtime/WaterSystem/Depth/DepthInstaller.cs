using Snm.DependencyInjection;

namespace Snm.WaterSystem.Depth
{
    public static class DepthInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<DepthHandle>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static DepthHandle Install(WaterFeatureContext ctx)
        {
            var feature = new DepthFeature(
                ctx.SurfaceMaterial,
                ctx.Config.depth,
                ctx.UpdateService);

            return new DepthHandle(feature);
        }
    }
}
