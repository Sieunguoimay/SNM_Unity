#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace ReflectionUsage
{
    public class ReflectionUsageFinderWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] private ReflectionUsage[] reflectionUsages;

        [MenuItem("Tools/ReflectionUsageFinderWindow")]
        private static void Open()
        {
            GetWindow<ReflectionUsageFinderWindow>().Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new ReflectionUsageFinderVE(this));
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            foreach (var p in ReflectionInfoProvider.SubProviders)
            {
                var ms = AssetDatabase.FindAssets($"t:MonoScript {p.GetType().Name}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>)
                    .FirstOrDefault();
                if (ms != null)
                {
                    menu.AddItem(new GUIContent($"Ping {ms.name}.cs"), false, () =>
                    {
                        EditorGUIUtility.PingObject(ms);
                    });
                }
            }
            menu.AddItem(new GUIContent($"Ping {nameof(ReflectionUsageFinderWindow)}.cs"), false, () =>
            {
                EditorGUIUtility.PingObject(
                    AssetDatabase.FindAssets($"t:MonoScript {nameof(ReflectionUsageFinderWindow)}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>)
                    .FirstOrDefault());
            });
        }

        public class ReflectionUsageFinderVE : VisualElement
        {
            private readonly TextField searchField;
            private readonly ListView scrollView;
            private readonly DropdownField dropdownField;
            private readonly ReflectionUsageFinderWindow window;
            private readonly Toggle loadSelectedObject;
            private ReflectionUsage[] Usages { get => window.reflectionUsages; set => window.reflectionUsages = value; }
            private static readonly string All = "All";
            public ReflectionUsageFinderVE(ReflectionUsageFinderWindow window)
            {

                loadSelectedObject = new Toggle() { text = "Only selected", value = false };
                Add(loadSelectedObject);

                var topBar = new VisualElement();
                topBar.style.flexDirection = FlexDirection.Row;
                topBar.style.height = 22;

                var loadButton = new Button() { text = "Load Reflection" };
                loadButton.RegisterCallback<ClickEvent>(OnLoadReflectionClicked);
                loadButton.style.height = 20;
                topBar.Add(loadButton);

                dropdownField = new DropdownField() { value = "All", tooltip = "Filter by Providers" };
                dropdownField.RegisterCallback<ClickEvent>(OnProviderButtonClicked);
                dropdownField.style.height = 20;
                topBar.Add(dropdownField);

                searchField = new TextField() { value = "" };
                searchField.RegisterCallback<KeyDownEvent>(OnKeyDownOnTextField);
                searchField.style.flexGrow = 1;
                searchField.style.height = 20;
                topBar.Add(searchField);

                var findButton = new Button() { text = "Find", tooltip = "Or Press Enter" };
                findButton.RegisterCallback<ClickEvent>(OnFindClicked);
                findButton.style.height = 20;
                topBar.Add(findButton);

                Add(topBar);

                Add(scrollView = new ListView());
                scrollView.style.marginTop = 20;
                style.flexDirection = FlexDirection.Column;
                this.window = window;

                UpdateDisplayList();
            }

            private void OnKeyDownOnTextField(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    UpdateDisplayList();
                }
            }

            private void OnFindClicked(ClickEvent evt)
            {
                UpdateDisplayList();
            }

            private void OnProviderButtonClicked(ClickEvent evt)
            {
                var menu = new GenericMenu();
                foreach (var p in ReflectionInfoProvider.SubProviders.Select(s => s.GetType().Name).Append(All).OrderBy(s => s))
                {
                    menu.AddItem(new GUIContent(p), dropdownField.value == p, () =>
                    {
                        dropdownField.value = p;
                        UpdateDisplayList();
                    });
                }
                menu.ShowAsContext();
            }

            private void OnLoadReflectionClicked(ClickEvent evt)
            {
                if (loadSelectedObject.value)
                {
                    var selectedObjects = Selection.objects
                        .Select(AssetDatabase.GetAssetPath)
                        .SelectMany(p => AssetDatabase.IsValidFolder(p) ? AssetDatabase.FindAssets("", new[] { p }).Select(AssetDatabase.GUIDToAssetPath) : new[] { p })
                        .Distinct()
                        .SelectMany(AssetDatabase.LoadAllAssetsAtPath);
                    Usages = LoadReflectionUsages(selectedObjects).ToArray();
                }
                else
                {
                    Usages = LoadReflectionUsages(IterateAllObjects()).ToArray();
                }

                UpdateDisplayList();

                for (int i = 0; i < Usages.Length; i++)
                {
                    UnityEngine.Debug.Log($"{i}. {Usages[i].GetText()}");
                }
            }

            private void UpdateDisplayList()
            {
                scrollView.UpdateDisplayList(Usages?.Where(u => SearchMatch(u, searchField?.value ?? "")) ?? Enumerable.Empty<ReflectionUsage>());
            }

            private bool SearchMatch(ReflectionUsage usage, string search)
            {
                return RegexMatch(search, usage.GetText()) && (usage.providers.Contains(dropdownField.value) || dropdownField.value == All);
            }

            public bool RegexMatch(string searchTerm, string str)
            {
                var regex = new Regex(string.Join(".*", searchTerm.Split(" ")), RegexOptions.IgnoreCase);
                return regex.IsMatch(str);
            }

            private static IEnumerable<ReflectionUsage> LoadReflectionUsages(IEnumerable<UnityEngine.Object> objs)
            {
                var i = 0;
                foreach (var o in objs)
                {
                    var u = GetReflectionUsage(o);
                    if (u != null)
                    {
                        u.index = i;
                        yield return u;
                        i++;
                    }
                }
            }

            static readonly List<ReflectionInfo> tempList = new();

            private static ReflectionUsage GetReflectionUsage(UnityEngine.Object o)
            {
                tempList.Clear();
                var providers = "";
                foreach (var p in ReflectionInfoProvider.SubProviders)
                {
                    var any = false;
                    foreach (var i in p.GetReflectionInfos(o))
                    {
                        tempList.Add(i);
                        any = true;
                    }
                    if (any)
                    {
                        providers += p.GetType().Name + " ";
                    }
                }
                if (tempList.Count > 0)
                {
                    return new ReflectionUsage
                    {
                        providers = providers,
                        path = AssetDatabase.GetAssetPath(o),
                        target = o,
                        reflections = tempList.ToArray()
                    };
                }
                return null;
            }

            private static IEnumerable<UnityEngine.Object> IterateAllObjects()
            {
                var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    var assets = Array.Empty<UnityEngine.Object>();
                    if (!path.EndsWith(".unity") && !path.EndsWith(".Unity"))
                    {
                        assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    }
                    foreach (var asset in assets)
                    {
                        if (asset != null)
                        {
                            yield return asset;
                        }
                    }
                }
            }

            private class ListView : ScrollView
            {
                private readonly ReflectionUsageEntryVE[] _usageVEs = new ReflectionUsageEntryVE[20];

                private ReflectionUsageEntryVE _selected;

                private IReadOnlyList<ReflectionUsage> _reflectionUsages;
                private readonly Label pageLabel;
                private readonly Label allLabel;
                private int _currentPageIndex;

                public ListView()
                {

                    var page = new VisualElement();
                    page.style.flexDirection = FlexDirection.Row;
                    page.style.alignItems = Align.Center;
                    page.style.alignContent = Align.Center;


                    var prev = new Button() { text = "<" };
                    prev.RegisterCallback<ClickEvent>(OnPrev);
                    page.Add(prev);

                    page.Add(pageLabel = new Label() { text = "0/0" });

                    var next = new Button() { text = ">" };
                    next.RegisterCallback<ClickEvent>(OnNext);
                    page.Add(next);

                    page.Add(allLabel = new Label() { text = "Count: 0" });
                    allLabel.style.alignSelf = Align.FlexEnd;
                    Add(page);

                    for (int i = 0; i < _usageVEs.Length; i++)
                    {
                        Add(_usageVEs[i] = new ReflectionUsageEntryVE());
                        _usageVEs[i].Clicked += OnItemClicked;
                    }
                }

                private void OnNext(ClickEvent evt)
                {
                    if (_reflectionUsages == null) return;
                    _currentPageIndex = Mathf.Min(_currentPageIndex + 1, Mathf.FloorToInt(_reflectionUsages.Count / 20f));
                    UpdatePaging();
                }
                private void OnPrev(ClickEvent evt)
                {
                    if (_reflectionUsages == null) return;
                    _currentPageIndex = Mathf.Max(_currentPageIndex - 1, 0);
                    UpdatePaging();
                }

                private void OnItemClicked(ReflectionUsageEntryVE u)
                {
                    _selected?.SetSelected(false);
                    _selected = u;
                    _selected?.SetSelected(true);
                }

                public void UpdateDisplayList(IEnumerable<ReflectionUsage> reflectionUsages)
                {
                    _reflectionUsages = reflectionUsages.ToArray();
                    _currentPageIndex = 0;
                    UpdatePaging();
                }

                private void UpdatePaging()
                {
                    if (_reflectionUsages == null) return;
                    allLabel.text = "Count: " + _reflectionUsages.Count;
                    pageLabel.text = $"{_currentPageIndex}/{Mathf.FloorToInt(_reflectionUsages.Count / 20f)}";
                    var it = _reflectionUsages.Skip(_currentPageIndex * 20).GetEnumerator();
                    for (var i = 0; i < _usageVEs.Length; i++)
                    {
                        if (it.MoveNext())
                        {
                            var r = it.Current;
                            _usageVEs[i].SetUsageData(r, i + _currentPageIndex * 20);
                        }
                        else
                        {
                            _usageVEs[i].SetUsageData(null, 0);
                        }
                    }
                }
            }


            private class ReflectionUsageEntryVE : VisualElement
            {
                private ReflectionUsage _data;
                private readonly Image icon;
                private readonly Label label;
                private readonly UnityEngine.Color bgColor;
                private readonly VisualElement right;
                private int _index;

                public event Action<ReflectionUsageEntryVE> Clicked;

                public ReflectionUsageEntryVE()
                {
                    style.flexDirection = FlexDirection.Row;
                    style.paddingTop = 5;
                    style.paddingBottom = 5;
                    style.paddingLeft = 5;
                    style.display = DisplayStyle.None;

                    icon = new Image();
                    icon.style.height = 16;
                    icon.style.width = 16;
                    Add(icon);

                    label = new Label();
                    label.style.flexGrow = 1;
                    label.style.whiteSpace = WhiteSpace.Normal;
                    label.style.width = new StyleLength(Length.Percent(90));
                    Add(label);

                    right = new VisualElement();
                    right.style.flexDirection = FlexDirection.Column;
                    right.style.alignItems = Align.Center;

                    var ping = new Button() { text = "Ping" };
                    ping.style.flexGrow = 1;
                    ping.style.width = 50;
                    ping.RegisterCallback<ClickEvent>(OnPing);
                    right.Add(ping);

                    var more = new Button() { text = "..." };
                    more.style.flexGrow = 1;
                    more.style.width = 50;
                    more.RegisterCallback<ClickEvent>(OnMoreClicked);
                    right.Add(more);

                    Add(right);

                    bgColor = style.backgroundColor.value;
                    RegisterCallback<ClickEvent>(OnClicked);

                    SetSelected(false);
                }

                private void OnMoreClicked(ClickEvent evt)
                {
                    var menu = new GenericMenu();
                    foreach (var r in _data.reflections)
                    {
                        var monoScript = AssetDatabase.FindAssets($"t:MonoScript {r.Type.Name}")
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>)
                            .FirstOrDefault();
                        if (monoScript != null)
                        {
                            menu.AddItem(new GUIContent($"Ping {monoScript.name}.cs"), false, () =>
                            {
                                EditorGUIUtility.PingObject(monoScript);
                            });
                        }
                    }
                    menu.ShowAsContext();
                }

                private void OnPing(ClickEvent evt)
                {
                    if (_data != null)
                    {
                        EditorGUIUtility.PingObject(_data.target);
                        TryOpenPrefabAndPingTarget();
                    }
                }

                private void TryOpenPrefabAndPingTarget()
                {
                    if (_data.path.EndsWith(".prefab"))
                    {
                        var ps = PrefabStageUtility.OpenPrefab(_data.path);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_data.path);
                        if (ReflectionUsage.FindLocalPathToGameObject(prefab, _data.target, out var localPath))
                        {
                            EditorGUIUtility.PingObject(GetGameObjectByPath(ps.prefabContentsRoot, localPath.Split("/"), 0));
                        }
                    }
                }

                public static GameObject GetGameObjectByPath(GameObject root, string[] path, int index)
                {
                    if (index < path.Length)
                    {
                        if (root.name == path[index])
                        {
                            if (index < path.Length - 1)
                            {
                                foreach (Transform t in root.transform)
                                {
                                    var found = GetGameObjectByPath(t.gameObject, path, index + 1);
                                    if (found != null)
                                    {
                                        return found;
                                    }
                                }
                            }
                            else
                            {
                                return root;
                            }
                        }
                    }
                    return null;
                }

                private void OnClicked(ClickEvent evt)
                {
                    if (_data != null)
                    {
                        Clicked?.Invoke(this);
                    }
                }

                public void SetUsageData(ReflectionUsage data, int index)
                {
                    _index = index;
                    _data = data;

                    if (_data != null)
                    {
                        style.display = DisplayStyle.Flex;
                        label.text = _data.GetText();
                        if (_data.target is Component)
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_data.path);
                            icon.image = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image;
                        }
                        else
                        {
                            icon.image = EditorGUIUtility.ObjectContent(_data.target, _data.target.GetType()).image;
                        }
                    }
                    else
                    {
                        style.display = DisplayStyle.None;
                    }
                }

                public void SetSelected(bool selected)
                {
                    style.backgroundColor = selected ? new UnityEngine.Color(.15f, .15f, .15f) : bgColor;
                    right.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
                    label.text = selected ? _data?.GetDetailText() : _data?.GetText();
                }
            }
        }
        [Serializable]
        private class ReflectionUsage
        {
            public string providers;
            public int index;
            public string path;
            public UnityEngine.Object target;
            public ReflectionInfo[] reflections;

            public string GetText()
            {
                return $"{path} - {target.name} ({target.GetType().Name}):\n {GetReflectionsText()}";
            }

            public string GetDetailText()
            {
                return $"<b>{path} - {target.name} ({target.GetType().Name}):</b>"
                    + $"\n - Local path: <i>{GetLocalPath()}</i>"
                    + $"\n - Reflections: <b>{GetReflectionsText()}</b>"
                    + $"\n - Providers: <i>{providers}</i>";
            }

            private string GetReflectionsText()
            {
                var str = string.Join(", ", reflections.Select(r => $"{r.Type.Name}.{r.member}"));
                return $"<color=#ffbb00>{str}</color>";
            }

            public string GetLocalPath()
            {
                var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (o is GameObject go && FindLocalPathToGameObject(go, target, out var p))
                {
                    return $"{p} ({target.GetType().Name})";
                }
                else
                {
                    return $"{target.name} ({target.GetType().Name})";
                }
            }

            public static bool FindLocalPathToGameObject(GameObject root, UnityEngine.Object target, out string path)
            {
                path = root.name;
                foreach (Transform t in root.transform)
                {
                    if (t == target || t.GetComponents<Component>().Contains(target))
                    {
                        path += "/" + t.name;
                        return true;
                    }
                    else
                    {
                        if (FindLocalPathToGameObject(t.gameObject, target, out var s))
                        {
                            path += "/" + s;
                            return true;
                        }
                    }
                }
                path = "";
                return false;
            }

        }
    }
}
#endif