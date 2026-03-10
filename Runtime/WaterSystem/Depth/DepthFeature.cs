using System;
using Snm.Reactivity;
using UnityEngine;

namespace Snm.WaterSystem.Depth
{
    public class DepthFeature : IUpdateTarget, IDisposable
    {
        private readonly WaterDepthConfig _config;
        private readonly IUpdateService _updateService;
        private readonly Effect _bindEffect;

        private readonly Signal<Color> _shallowColor;
        private readonly Signal<Color> _deepColor;
        private readonly Signal<float> _absorption;

        public DepthFeature(
            Material material,
            WaterDepthConfig config,
            IUpdateService updateService)
        {
            _config = config;
            _updateService = updateService;

            _shallowColor = new Signal<Color>(config.shallowColor);
            _deepColor = new Signal<Color>(config.deepColor);
            _absorption = new Signal<float>(config.absorption);

            var binder = new DepthShaderBinder(material);

            _bindEffect = new Effect(() =>
            {
                binder.Bind(
                    _shallowColor.Value,
                    _deepColor.Value,
                    _absorption.Value);
            });

            updateService.AddUpdateTarget(this);
        }

        public void Update(float deltaTime)
        {
            _shallowColor.Value = _config.shallowColor;
            _deepColor.Value = _config.deepColor;
            _absorption.Value = _config.absorption;
        }

        public void Dispose()
        {
            _bindEffect.Dispose();
            _updateService.RemoveUpdateTarget(this);
        }
    }
}
