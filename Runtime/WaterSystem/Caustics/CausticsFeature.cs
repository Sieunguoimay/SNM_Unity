using System;
using Snm.Reactivity;
using UnityEngine;

namespace Snm.WaterSystem.Caustics
{
    public class CausticsFeature : IUpdateTarget, IDisposable
    {
        private readonly CausticsConfig _config;
        private readonly IUpdateService _updateService;
        private readonly Effect _bindEffect;

        private readonly Signal<Texture2D> _texture;
        private readonly Signal<float> _strength;
        private readonly Signal<float> _scale;
        private readonly Signal<float> _speed;
        private readonly Signal<float> _split;

        public CausticsFeature(
            Material material,
            CausticsConfig config,
            IUpdateService updateService)
        {
            _config = config;
            _updateService = updateService;

            _texture = new Signal<Texture2D>(config.causticsTexture);
            _strength = new Signal<float>(config.strength);
            _scale = new Signal<float>(config.scale);
            _speed = new Signal<float>(config.speed);
            _split = new Signal<float>(config.split);

            var binder = new CausticsShaderBinder(material);

            _bindEffect = new Effect(() =>
            {
                binder.Bind(
                    _texture.Value,
                    _strength.Value,
                    _scale.Value,
                    _speed.Value,
                    _split.Value);
            });

            updateService.AddUpdateTarget(this);
        }

        public void Update(float deltaTime)
        {
            _texture.Value = _config.causticsTexture;
            _strength.Value = _config.strength;
            _scale.Value = _config.scale;
            _speed.Value = _config.speed;
            _split.Value = _config.split;
        }

        public void Dispose()
        {
            _bindEffect.Dispose();
            _updateService.RemoveUpdateTarget(this);
        }
    }
}
