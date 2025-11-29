using UnityEngine;

namespace Snm.Visual.Noises
{

    public class NoiseMover : MonoBehaviour
    {
        [SerializeField] private float amplitude = 1f;
        [SerializeField] private float speed = 1f;
        [SerializeField] private Texture2D noiseTexture;
        [SerializeField] private Transform target;

        private float _startTime = 0f;

        private void Start()
        {
            _startTime = Time.time * Random.value;// from 0 -> Time.time
        }

        private void Update()
        {
            var x = (Time.time - _startTime) * speed;
            var y = Mathf.Floor(x) / noiseTexture.height;//scroll
            var offset = new Vector3(
                (EvaluateNoise(x, y) - .5f) * amplitude,
                (EvaluateNoise(x, y + .25f) - .5f) * amplitude,
                (EvaluateNoise(x, y + .5f) - .5f) * amplitude
            );
            target.localPosition = offset;
        }

        private float EvaluateNoise(float x, float y)
        {
            return noiseTexture.GetPixelBilinear(x, y).r;
        }

    }
}
