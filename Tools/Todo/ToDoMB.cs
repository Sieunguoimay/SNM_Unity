using UnityEngine;
public class ToDoMB : MonoBehaviour, IToDo
{
    [TextArea]
    [SerializeField] private string toDo;
    [SerializeField] private bool done;

    public string Text => toDo;
    public bool Done => done;
    public bool Editable => false;
    public Object Target => this;

    private void Start()
    {
        if (!done)
        {
            Debug.Log($"ToDo: {toDo}", this);
        }
    }
}
