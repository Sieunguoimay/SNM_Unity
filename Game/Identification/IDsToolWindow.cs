using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;

#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace Identification
{

#if UNITY_EDITOR
    public partial class IDsToolWindow : EditorWindow
    {
        [SerializeField] private MonoScript idSpaceScript;
        [SerializeField] private List<ID> ids = new();

        [MenuItem("Tools/Game/IDsTool")]
        public static void OpenWindow()
        {
            GetWindow<IDsToolWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new ToolVE(this));
        }

        private class ToolVE : VisualElement
        {
            private readonly IDsToolWindow window;
            private readonly VisualElement container;
            private readonly ObjectField script;

            public ToolVE(IDsToolWindow window)
            {
                this.window = window;
                window.LoadIDSpace();

                var bottom = new VisualElement();
                bottom.style.flexDirection = FlexDirection.Row;
                bottom.style.minHeight = UnityEditor.EditorGUIUtility.singleLineHeight;
                var top = new VisualElement();
                top.style.flexDirection = FlexDirection.Row;
                top.style.minHeight = UnityEditor.EditorGUIUtility.singleLineHeight;

                script = new UnityEditor.UIElements.ObjectField
                {
                    name = "Script",
                    objectType = typeof(MonoScript),
                    value = window.idSpaceScript
                };
                script.RegisterValueChangedCallback(OnScriptChanged);
                script.style.flexGrow = 1;

                var select = new Button() { text = "..." };
                select.RegisterCallback<ClickEvent>(evt => OnSelect());

                var refresh = new Button() { text = "Reload" };
                refresh.RegisterCallback<ClickEvent>(evt => OnReload());

                var addNew = new Button() { text = "Add New" };
                addNew.RegisterCallback<ClickEvent>(evt => OnAddNew());
                addNew.style.flexGrow = 1;

                var export = new Button() { text = "Export" };
                export.RegisterCallback<ClickEvent>(evt => OnExport());
                export.style.flexGrow = 1;

                Add(top);
                top.Add(script);
                top.Add(select);
                top.Add(refresh);
                Add(container = new ScrollView() { });
                Add(bottom);
                bottom.Add(addNew);
                bottom.Add(export);
                PopulateIDVEs();
            }

            private void OnSelect()
            {
                var gm = new GenericMenu();
                foreach (var t in GetTypesWithAttribute<IDSpaceAttribute>())
                {
                    var isCurrent = window.idSpaceScript != null && window.idSpaceScript.GetClass() == t;
                    gm.AddItem(new GUIContent(t.Name), isCurrent, () =>
                    {
                        script.value = AssetDatabase.FindAssets($"t:MonoScript {t.Name}")
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                            .Where(ms => ms.GetClass().GetAttribute<IDSpaceAttribute>() != null)
                            .FirstOrDefault();
                        window.LoadIDSpace();
                        Refresh();
                    });
                }
                gm.ShowAsContext();
            }
            private void OnExport()
            {
                window.Export();
            }

            private void OnAddNew()
            {
                window.AddNew();
                Refresh();
            }

            private void OnScriptChanged(ChangeEvent<UnityEngine.Object> evt)
            {
                window.idSpaceScript = evt.newValue as MonoScript;
            }

            private void OnReload()
            {
                window.LoadIDSpace();
                Refresh();
            }

            private void Refresh()
            {
                container.Clear();
                PopulateIDVEs();
            }

            private void PopulateIDVEs()
            {
                for (int i = 0; i < window.ids.Count; i++)
                {
                    var id = window.ids[i];
                    container.Add(new IDVE(id, i, this));
                }
            }

            private class IDVE : VisualElement
            {
                public IDVE(ID id, int index, ToolVE tool)
                {
                    style.flexDirection = FlexDirection.Row;
                    style.alignItems = Align.Center;
                    var obj = id.IdentifiedObject != null ? $" -> {id.IdentifiedObject}" : "";
                    var label = new Label($"{index}. {id.Value}{obj}");
                    label.style.flexGrow = 1;
                    var btn = new Button() { text = "X" };
                    btn.style.width = 20;
                    btn.RegisterCallback<ClickEvent>(evt =>
                    {
                        tool.window.ids.Remove(id);
                        tool.Refresh();
                    });
                    Add(label);
                    Add(btn);
                }
            }

        }

        private void LoadIDSpace()
        {
            if (idSpaceScript == null) return;
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            var t = idSpaceScript.GetClass();
            ids = t.GetProperties(flags)
                .Where(p => p.PropertyType == typeof(ID))
                .Select(p => p.GetValue(null))
                .OfType<ID>().ToList();
            var ct = t.GetField("Counter");
        }

        [ContextMenu("AddNew")]
        private void AddNew()
        {
            if (idSpaceScript == null) return;
            ids.Add(new ID(GenerateGUID()));
        }

        [ContextMenu("Export")]
        private void Export()
        {
            if (idSpaceScript == null) return;

            var path = AssetDatabase.GetAssetPath(idSpaceScript);
            var absolutePath = path.Replace("Assets", Application.dataPath);
            var cs = string.Join("\n", CreateCSLines());

            File.WriteAllText(absolutePath, cs);

            AssetDatabase.Refresh();

            Debug.Log($"{absolutePath}");
        }

        private IEnumerable<string> CreateCSLines()
        {
            var tab = "    ";
            yield return $"namespace Identification";
            yield return $"{{";
            yield return $"{tab}[IDSpace]";
            yield return $"{tab}public class {idSpaceScript.name}";
            yield return $"{tab}{{";
            foreach (var id in ids)
            {
                yield return $"{tab}{tab}public static ID _{id.Value} {{ get; }} = new(\"{id.Value}\");";
            }
            yield return $"{tab}}}";
            yield return $"}}";
        }

        public string GenerateGUID()
        {
            return System.Guid.NewGuid().ToString()[..8];
        }

        public static Type[] GetTypesWithAttribute<T>() where T : Attribute
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            var result = types.Where(type => type.GetCustomAttributes(typeof(T), false).Any()).ToArray();

            return result;
        }

    }
#endif
}