using Snm.DependencyInjection;
using Snm.Runtime.Unity;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Snm.WaterSystem.Wave
{
    public static class WaveSimulationInstaller
    {
        public static void Install(
            IBindingContext container,
            WaveSimulationConfig config,
            int textureSize,
            Shader simulationShader)
        {
            container.Bind<IWaveSimulation>()
                .ToScoped(r =>
                {
                    var ctx = r.Resolve<WaterFeatureContext>();
                    var simMaterial = new Material(simulationShader);
                    var displayMaterial = ctx.SurfaceMaterial;
                    return WaveSimulationFactory.Create(config, textureSize, simMaterial, displayMaterial);
                });
        }
    }
}
