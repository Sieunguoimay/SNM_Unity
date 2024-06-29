#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Tools
{
    public class AssetUsageFinderWindow : EditorWindow
    {
        [MenuItem("Tools/AssetUsageFinder")]
        private static void Open()
        {
            GetWindow<AssetUsageFinderWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new AssetUsageFinderVE());
        }

        private class AssetUsageFinderVE : VisualElement
        {
            private readonly DisplayListView display;
            private readonly ObjectField target;
            private readonly Toggle findInSelected;
            public AssetUsageFinderVE()
            {
                findInSelected = new Toggle() { value = false, label = "Find In Selected" };
                Add(findInSelected);

                target = new ObjectField() { label = "Target" };
                Add(target);

                var button = new Button() { text = "Find Usages" };
                button.RegisterCallback<ClickEvent>(OnFindDependentsButtonClicked);
                Add(button);

                var missings = new Button() { text = "Find Missings" };
                missings.RegisterCallback<ClickEvent>(OnFindMissingsButtonClicked);
                Add(missings);

                display = new DisplayListView();
                Add(display);
            }

            private void OnFindMissingsButtonClicked(ClickEvent evt)
            {
                foreach (var ap in GetAssetPaths())
                {
                    var guids = AssetUsageHelper.GetReferences(ap);
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid.Item1);
                        if (string.IsNullOrEmpty(path))
                        {
                            Debug.Log($"There are Missings at {ap} {guid}", AssetDatabase.LoadAssetAtPath<Object>(ap));
                        }
                    }
                }
            }

            private void OnFindDependentsButtonClicked(ClickEvent evt)
            {
                var obj = target.value;

                if (obj == null) return;
                var dependents = AssetUsageHelper.GetAllDependents(obj, GetAssetPaths()).ToArray();

                display.SetDisplayData(new DisplayListData
                {
                    target = obj,
                    dependents = dependents
                });

                AssetUsageHelper.LogAssetUsages(obj, dependents);
            }

            private IEnumerable<string> GetAssetPaths()
            {
                var selectedPaths = Selection.objects.Select(AssetDatabase.GetAssetPath)
                    .SelectMany(p => AssetDatabase.IsValidFolder(p) ? AssetDatabase.FindAssets("", new[] { p }).Select(AssetDatabase.GUIDToAssetPath) : new[] { p })
                    .Distinct();
                return findInSelected.value ? selectedPaths : AssetUsageHelper.GetAllAssetPaths();
            }
        }

        private class DisplayListView : VisualElement
        {
            private readonly VisualElement topBar;
            // private readonly ObjectField objectField;
            private readonly ScrollView scrollView;
            private DisplayListData _data;
            public DisplayListView()
            {
                topBar = new VisualElement();
                topBar.style.flexDirection = FlexDirection.Row;
                topBar.style.marginLeft = 5;

                // objectField = new ObjectField() { label = "Usages of object" };
                // topBar.Add(objectField);
                Add(topBar);

                scrollView = new ScrollView();
                Add(scrollView);
            }

            public void SetDisplayData(DisplayListData data)
            {
                _data = data;
                TryUpdateView();
            }

            private void TryUpdateView()
            {
                if (_data != null)
                {
                    // objectField.value = _data.target;
                    scrollView.Clear();
                    foreach (var p in _data.dependents)
                    {
                        scrollView.Add(new DisplayItem(p, _data.target));
                    }
                }
            }
        }

        private class DisplayItem : VisualElement
        {
            private readonly string path;
            private readonly UnityEngine.Object target;
            private readonly Label label;
            private readonly VisualElement sub;
            public DisplayItem(string path, UnityEngine.Object target)
            {
                this.path = path;
                this.target = target;

                var horizontal = new VisualElement();
                horizontal.style.flexDirection = FlexDirection.Row;

                label = new Label { text = path };
                label.style.flexGrow = 1;
                horizontal.Add(label);

                var ping = new Button() { text = "Ping" };
                ping.RegisterCallback<ClickEvent>(OnPingClicked);
                horizontal.Add(ping);

                Add(horizontal);

                sub = new VisualElement();
                Add(sub);

                label.RegisterCallback<ClickEvent>(OnClicked);
            }

            private void OnPingClicked(ClickEvent evt)
            {
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }

            private void OnClicked(ClickEvent evt)
            {
                sub.Clear();
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj is GameObject)
                {
                    var ps = PrefabStageUtility.OpenPrefab(path);
                    foreach (var c in FindDependents(ps.prefabContentsRoot, target))
                    {
                        var label = new Label() { text = " - " + c.Item2 };
                        label.RegisterCallback<ClickEvent>(evt => { EditorGUIUtility.PingObject(c.Item1); });
                        sub.Add(label);
                    }
                }
            }

            public static IEnumerable<(UnityEngine.Object, string)> FindDependents(GameObject root, UnityEngine.Object target)
            {
                foreach (var c in IterateAllObject(root, root.name))
                {
                    var so = new SerializedObject(c.Item1);
                    so.Update();
                    var it = so.GetIterator();
                    while (it.Next(true))
                    {
                        if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null && it.objectReferenceValue == target)
                        {
                            yield return (c.Item1, $"{c.Item2} {c.Item1.GetType().Name}.{it.propertyPath}");
                            break;
                        }
                    }
                }
            }
            public static IEnumerable<(UnityEngine.Object, string)> IterateAllObject(GameObject go, string path)
            {
                yield return (go, path);
                foreach (var c in go.GetComponents<Component>())
                {
                    yield return (c, path);
                }
                foreach (Transform t in go.transform)
                {
                    foreach (var c in IterateAllObject(t.gameObject, path + "/" + t.name))
                    {
                        yield return c;
                    }
                }
            }
        }

        private class DisplayListData
        {
            public UnityEngine.Object target;
            public string[] dependents;
        }
    }
}
#endif