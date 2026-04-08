using UnityEngine;

namespace Snm.CameraRig
{
    [CreateAssetMenu(fileName = "CameraRigConfig", menuName = "Config/Camera Rig Config")]
    public class CameraRigConfigSO : ScriptableObject
    {
        [SerializeField] private CameraRigConfig config;

        public CameraRigConfig Config => config;
    }
}
