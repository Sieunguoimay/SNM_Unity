using UnityEngine;

namespace ObjectLocate
{
    [System.Obsolete]
    public class LocatedObjectProvider : MonoBehaviour
    {
        [SerializeField] private ObjectLocator objectLocator;
        [ObjectSelector]
        [SerializeField] private Object obj;

        private void Awake()
        {
            objectLocator.SetObject(obj);
        }
    }
}