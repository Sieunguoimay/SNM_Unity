#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ToDoWindow : EditorWindow
{
    private VisualElement _container;
    [MenuItem("Tools/ToDo")]
    private static void Open()
    {
        GetWindow<ToDoWindow>().Show();
    }
    private void CreateGUI()
    {
        var toolBar = new ToolBar();
        toolBar.RefreshButton.RegisterCallback<ClickEvent>(OnRefreshClicked);
        toolBar.FilterButton.RegisterCallback<ClickEvent>(OnFilterClicked);
        rootVisualElement.Add(toolBar);

        rootVisualElement.Add(_container = new VisualElement());
        PopulateToDoListVE();
    }

    private void OnFilterClicked(ClickEvent evt)
    {
    }

    private void OnRefreshClicked(ClickEvent evt)
    {
        _container.Clear();
        PopulateToDoListVE();
    }

    private void PopulateToDoListVE()
    {
        _container.Add(new ToDoListVE(new ToDoAssetList()));

        foreach (var t in GetAllToDoLists())
        {
            _container.Add(new ToDoListVE(t));

        }
    }

    public class ToolBar : VisualElement
    {
        public Button RefreshButton { get; }
        public Button FilterButton { get; }

        public ToolBar()
        {
            style.flexDirection = FlexDirection.Row;
            Add(RefreshButton = new Button() { text = "Refresh" });
            Add(FilterButton = new Button() { text = "Filter" });
        }

    }

    private class ToDoListVE : VisualElement
    {
        private readonly IToDoList toDoList;

        public ToDoListVE(IToDoList toDoList)
        {
            this.toDoList = toDoList;
            Add(new Label(toDoList.Name));
            foreach (var item in toDoList.ToDoItems)
            {
                Add(new ToDoLine(item));
            }
        }
    }
    private class ToDoLine : VisualElement
    {
        private readonly Toggle tick;
        private readonly TextField text;
        private readonly IToDoEditable toDoEditable;

        public ToDoLine(IToDo todo)
        {
            tick = new Toggle() { value = todo.Done };
            text = new TextField() { value = todo.Text };
            text.style.flexGrow = 1;
            Add(tick);
            Add(text);
            style.flexDirection = FlexDirection.Row;

            if (todo is IToDoEditable tde)
            {
                toDoEditable = tde;
                tick.RegisterCallback<ChangeEvent<bool>>(OnTickChanged);
                text.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
            }
            else
            {
                SetEnabled(false);
            }
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            toDoEditable.SetText(evt.newValue);
        }

        private void OnTickChanged(ChangeEvent<bool> evt)
        {
            toDoEditable.SetDone(evt.newValue);
        }
    }

    private IEnumerable<IToDoList> GetAllToDoLists()
    {
        return AssetDatabase.FindAssets("", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.EndsWith("unity"))
            .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
            .OfType<IToDoList>();
    }

    private class ToDoAssetList : IToDoList
    {
        public ICollection<IToDo> _toDoItems;
        public ICollection<IToDo> ToDoItems => _toDoItems ??= GetAllToDos().ToArray();

        public bool Editable => false;

        public string Name => "ToDos in Assets";

        public void AddItem(IToDo item)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveItem(IToDo item)
        {
            throw new System.NotImplementedException();
        }

        private IEnumerable<IToDo> GetAllToDos()
        {
            return AssetDatabase.FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.EndsWith("unity"))
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<IToDo>();
        }
    }
}
#endif