using Snm.Runtime.Unity;
using UnityEngine;

namespace Snm.Components
{
    public class PublicMonoBehaviour : MonoBehaviour
    {
        private static PublicMonoBehaviour _instance;
        public static PublicMonoBehaviour Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_isDestroyed) return null;

                    _instance = UnityEngineUtility.CreateGameObjectWithComponent<PublicMonoBehaviour>();

                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        private static bool _isDestroyed = false;

        void OnDestroy()
        {
            if (this != _instance) return;

            _isDestroyed = true;
        }
    }
}