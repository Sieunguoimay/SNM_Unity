using Snm.DependencyInjection;

namespace Snm.WaterSystem.Sparkle
{
    public static class SparkleInstaller
    {
        public static void Install(IBindingContext container)
        {
            container.Bind<SparkleFeature>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    return Install(ctx);
                });
        }

        public static SparkleFeature Install(WaterFeatureContext ctx)
        {
            return new SparkleFeature(
                ctx.SurfaceMaterial,
                ctx.Config.sparkle);
        }
    }
}
