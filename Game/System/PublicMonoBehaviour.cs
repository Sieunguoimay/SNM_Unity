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
                if (_isDestroyed) return null;
                _instance = new GameObject(nameof(PublicMonoBehaviour) + "_Singleton").AddComponent<PublicMonoBehaviour>();
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

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static bool IsAwakened { get; private set; }
    public static bool IsStarted { get; private set; }
    public static bool IsDestroyed { get; private set; }
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                if (IsDestroyed) return null;

                _instance = FindExistingInstance() ?? CreateNewInstance();
            }
            return _instance;
        }
    }

    #region Singleton Implementation

    private static T _instance;

    private static T FindExistingInstance()
    {
        T[] existingInstances = FindObjectsOfType<T>();

        // No instance found
        if (existingInstances == null || existingInstances.Length == 0) return null;

        return existingInstances[0];
    }

    private static T CreateNewInstance()
    {
        var containerGO = new GameObject("__" + typeof(T).Name + " (Singleton)");
        return containerGO.AddComponent<T>();
    }

    #endregion

    #region Singleton Life-time Management

    protected virtual void SingletonAwakened() { }

    protected virtual void SingletonStarted() { }

    protected virtual void SingletonDestroyed() { }

    protected virtual void NotifyInstanceRepeated()
    {
        Component.Destroy(this.GetComponent<T>());
    }

    #endregion

    #region Unity3d Messages - DO NOT OVERRRIDE / IMPLEMENT THESE METHODS in child classes!
    void Awake()
    {
        T thisInstance = this.GetComponent<T>();

        // Initialize the singleton if the script is already in the scene in a GameObject
        if (_instance == null)
        {
            _instance = thisInstance;
            DontDestroyOnLoad(_instance.gameObject);

        }

        else if (thisInstance != _instance)
        {
            Debug.Log(string.Format(
                "Found a duplicated instance of a Singleton with type {0} in the GameObject {1}",
                this.GetType(), this.gameObject.name));

            NotifyInstanceRepeated();

            return;
        }


        if (!IsAwakened)
        {
            Debug.Log(string.Format(
                "Awake() Singleton with type {0} in the GameObject {1}",
                this.GetType(), this.gameObject.name));

            SingletonAwakened();
            IsAwakened = true;
        }

    }

    void Start()
    {
        // do not start it twice
        if (IsStarted) return;

        Debug.Log(string.Format(
            "Start() Singleton with type {0} in the GameObject {1}",
            this.GetType(), this.gameObject.name));

        SingletonStarted();
        IsStarted = true;
    }

    void OnDestroy()
    {
        if (this != _instance) return;

        IsDestroyed = true;

        Debug.Log(string.Format(
            "Destroy() Singleton with type {0} in the GameObject {1}",
            this.GetType(), this.gameObject.name));
        SingletonDestroyed();
    }

    #endregion
}