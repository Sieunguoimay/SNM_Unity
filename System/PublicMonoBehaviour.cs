using UnityEngine;

public class PublicMonoBehaviour : MonoBehaviour
{
    private static PublicMonoBehaviour _instance;
    public static PublicMonoBehaviour Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("Create PublicMonoBehaviour");
                _instance = new GameObject("PublicMonoBehaviour").AddComponent<PublicMonoBehaviour>();
            }
            return _instance;
        }
    } 
}