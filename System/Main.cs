using Supports.ViewHierachy;
using UnityEngine;

public class Main : MonoBehaviour
{
    [SerializeField] private GameSO prototypeManager;
    [SerializeField] private ViewNode prototypePrefab;

    public static Main Instance { get; private set; }

    public GameSO PrototypeManager => prototypeManager;

    private ViewNode _prototypeVisual;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        prototypeManager.Setup();

        _prototypeVisual = Instantiate(prototypePrefab, transform);

        _prototypeVisual.Setup(prototypeManager);
    }

    private void OnDestroy()
    {
        _prototypeVisual.TearDown();

        Destroy(_prototypeVisual);

        prototypeManager.TearDown();
    }
}
