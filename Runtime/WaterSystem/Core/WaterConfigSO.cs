using UnityEngine;

namespace Snm.WaterSystem
{
    [CreateAssetMenu(fileName = "WaterConfig", menuName = "Config/Water Config")]
    public class WaterConfigSO : ScriptableObject
    {
        [SerializeField] private WaterConfig config;

        public WaterConfig Config => config;
    }
}
