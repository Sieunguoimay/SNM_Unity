using UnityEngine;

namespace ObjectLocate
{
    [System.Obsolete]
    public class ObjectLocator : ScriptableObject
    {
        private Object _object;

        public TObject GetObject<TObject>() where TObject : Object
        {
            if (_object is TObject obj)
            {
                return obj;
            }
            Debug.LogError($"Failed to GetObject of type {typeof(TObject).Name}. Current object is {_object}");
            return default;
        }

        public void SetObject(Object obj)
        {
            _object = obj;
        }
    }
}