using UnityEngine;

namespace FruitCollectorGame
{
    public class MonoSingleton<TObject> : MonoBehaviour where TObject : MonoBehaviour
    {
        private static TObject _instance;
        public static TObject Instance
        {
            get
            {
                if (IsSingletonDestroyed) return null;

                if (_instance == null)
                {
                    _instance = new GameObject("[Singleton]" + typeof(TObject).Name).AddComponent<TObject>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        protected static bool IsSingletonDestroyed { get; private set; } = false;

        private void OnDestroy()
        {
            IsSingletonDestroyed = true;
        }
    }
}
