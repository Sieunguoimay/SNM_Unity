using System.Collections.Generic;
using UnityEngine;
public interface IToDoList
{
    string Name { get; }
    ICollection<IToDo> ToDoItems { get; }
    bool Editable { get; }
    void AddItem(IToDo item);
    void RemoveItem(IToDo item);
}

public partial class ToDoListSO : ScriptableObject, IToDoList
{
    [SerializeField] private ToDoItem[] toDoItems;
    public ICollection<IToDo> ToDoItems => toDoItems;

    public bool Editable => true;

    public string Name => name;

    public void AddItem(IToDo item)
    {
        System.Array.Resize(ref toDoItems, toDoItems.Length + 1);
        toDoItems[^1] = new ToDoItem(item);
    }

    public void RemoveItem(IToDo item)
    {
        var index = System.Array.IndexOf(toDoItems, item);
        if (index >= 0)
        {
            toDoItems[index] = toDoItems[^1];
            System.Array.Resize(ref toDoItems, toDoItems.Length - 1);
        }
    }

    [System.Serializable]
    private class ToDoItem : IToDoEditable
    {
        [SerializeField] private string text;
        [SerializeField] private bool done;

        public ToDoItem(IToDo item)
        {
            text = item.Text;
            done = item.Done;
        }

        public string Text => text;
        public bool Done => done;

        public bool Editable => true;

        public Object Target { get; private set; }

        public void SetDone(bool done)
        {
            this.done = done;
        }

        public void SetTarget(Object target)
        {
            Target = target;
        }

        public void SetText(string text)
        {
            this.text = text;
        }
    }
}
