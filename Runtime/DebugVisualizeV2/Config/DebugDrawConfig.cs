using TMPro;
using UnityEngine;

namespace Snm.Runtime.DebugDraw
{
    [CreateAssetMenu(fileName = "DebugDrawConfig", menuName = "DebugDraw/Config")]
    public class DebugDrawConfig : ScriptableObject
    {
        [Header("General")]
        public bool enabled = true;

        [Header("Pool Sizes")]
        public int linePoolSize  = 100;
        public int labelPoolSize = 100;
        public int meshPoolSize  = 50;

        [Header("Defaults")]
        public float   lineWidth         = 0.05f;
        public float   fontSize          = 3f;
        public Vector3 labelOffset       = default; // set via Reset()
        public bool    autoHideOffScreen = true;

        [Header("Colors")]
        public Color lineColor    = Color.yellow;
        public Color arrowColor   = Color.magenta;
        public Color sphereColor  = Color.red;
        public Color boxColor     = Color.cyan;
        public Color circleColor  = Color.green;
        public Color labelColor   = Color.white;
        public Color barBgColor   = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public Color barFillColor = Color.green;

        [Header("Font")]
        public TMP_FontAsset font;

        private void Reset()
        {
            labelOffset = Vector3.up * 2f;
        }
    }
}
