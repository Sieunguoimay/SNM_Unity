using Snm.DependencyInjection;

namespace Snm.WaterSystem.Shoreline
{
    public static class ShorelineInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<ShorelineFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static ShorelineFeature Install(WaterFeatureContext ctx)
        {
            return new ShorelineFeature(
                ctx.SurfaceMaterial,
                ctx.Config.shoreline);
        }
    }
}
