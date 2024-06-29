using UnityEngine;

public class NoiseMover : MonoBehaviour, IAnimationCurveEvaluateHandler
{
    [SerializeField] private float amplitude = 1f;
    [SerializeField] private float speed = 1f;
    [SerializeField] private Texture2D noiseTexture;

    private float _startTime = 0f;

    private void Start()
    {
        _startTime = Time.time * Random.value;// from 0 -> Time.time
    }

    //Runtime Amplitude
    public void OnEvaluated(float time, float value)
    {
        if (Application.isPlaying)
        {
            amplitude = value;
        }
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
        transform.localPosition = offset;
    }

    private float EvaluateNoise(float x, float y)
    {
        return noiseTexture.GetPixelBilinear(x, y).r;
    }

}
