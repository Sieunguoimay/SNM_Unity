#if UNITY_EDITOR

namespace Snm.Tools.InspectorExtra
{
    [System.Serializable]
    public class ObjectInfo
    {
        public string path = "";
        public string localId = "";
        public ObjectType objectType;

        public string Path => string.IsNullOrEmpty(path) ? "NULL" : $"{path.Replace("/", "\u2215")}";
        public string Display => string.IsNullOrEmpty(path) ? "NULL" : $"{path.Replace("/", "\u2215")}|{localId}|{objectType}";

        public static bool Equals(ObjectInfo x, ObjectInfo y)
        {
            if (x == null || y == null) return false;
            return x.path == y.path && x.localId == y.localId && x.objectType == y.objectType;
        }
    }

    public enum ObjectType
    {
        NonPrefabAsset,
        ObjectInPrefab,
        ObjectInScene,
    }
}

#endif