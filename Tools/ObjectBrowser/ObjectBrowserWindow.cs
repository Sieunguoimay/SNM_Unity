#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Snm.Tools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Snm.Tools.ObjectBrowser
{
    public class ObjectBrowserWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] private Object rootObject;
        [SerializeField] private string path;
        [SerializeField] private ReflectionFilterType reflectionFilterType = ReflectionFilterType.IncludeBaseTypes;
        [SerializeField] private MemberFilterType memberFilterType = MemberFilterType.AllMembers;
        [SerializeField] private bool displayTypeHash;
        [SerializeField] private bool watchMode;
        [SerializeField] private string memberSearchFilter = "";

        private IReadOnlyList<ObjectExposedItem> _displayItems;
        private object _currentObject;
        private Type _currentReflectionType;
        private Vector2 _scrollPos;

        // Navigation history
        private readonly List<string> _pathHistory = new();
        private int _historyCursor = -1;
        private bool _navigating;

        // Watch mode
        private double _lastRefreshTime;
        private const double WatchInterval = 0.5;

        public object CurrentObject => _currentObject;
        public string Path => path;
        public Object RootObject => rootObject;

        public event Action<ObjectBrowserWindow> OnExposed;
        public event Action<ObjectBrowserWindow> OnClosed;

        [MenuItem("Tools/Snm/Object Browser")]
        public static void OpenWindow() => OpenWindowAndReturnSelf();

        public static ObjectBrowserWindow OpenWindowAndReturnSelf()
        {
            return GetWindow<ObjectBrowserWindow>("Object Browser");
        }

        private void OnEnable()
        {
            Browse();
        }

        private void OnDestroy()
        {
            OnClosed?.Invoke(this);
        }

        private void Update()
        {
            if (watchMode && EditorApplication.isPlaying && EditorApplication.timeSinceStartup - _lastRefreshTime > WatchInterval)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Browse();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawSourceObject();
            GUILayout.Space(4);
            DrawBreadcrumb();
            GUILayout.Space(4);
            DrawFilterBar();
            DrawItems();
            DrawEmptyState();
        }

        // ── Source Object ────────────────────────────────────────

        private void DrawSourceObject()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50);
            var ro = EditorGUILayout.ObjectField("Source Object", rootObject, typeof(Object), true);
            DrawComponentSelectingButton(rootObject, i =>
            {
                ChangeRootObject(i);
                ResetPath();
                Browse();
            });

            if (GUILayout.Button(new GUIContent("@", "Pick in-memory object"), GUILayout.Width(25)))
                PickInMemory();

            // Watch toggle
            var watchLabel = watchMode ? "Watch: ON" : "Watch: OFF";
            var watchStyle = new GUIStyle(EditorStyles.miniButton);
            if (watchMode) watchStyle.normal.textColor = new Color(0.3f, 0.9f, 0.3f);
            if (GUILayout.Button(watchLabel, watchStyle, GUILayout.Width(70)))
            {
                watchMode = !watchMode;
                _lastRefreshTime = 0;
            }

            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(50)))
                Browse();

            EditorGUILayout.EndHorizontal();

            if (rootObject != ro)
            {
                ChangeRootObject(ro);
                ResetPath();
                Browse();
            }
        }

        // ── Breadcrumb ───────────────────────────────────────────

        private void DrawBreadcrumb()
        {
            EditorGUILayout.BeginHorizontal();

            // Back / Forward
            GUI.enabled = _historyCursor > 0;
            if (GUILayout.Button("\u25C0", EditorStyles.miniButton, GUILayout.Width(22)))
                NavigateHistory(-1);
            GUI.enabled = _historyCursor < _pathHistory.Count - 1;
            if (GUILayout.Button("\u25B6", EditorStyles.miniButton, GUILayout.Width(22)))
                NavigateHistory(1);
            GUI.enabled = true;

            var segments = GetPathSegments();

            // Root button
            var rootStyle = new GUIStyle(EditorStyles.miniButton) { fontStyle = segments.Length == 0 ? FontStyle.Bold : FontStyle.Normal, stretchWidth = false };
            if (GUILayout.Button(rootObject != null ? rootObject.name : "root", rootStyle))
            {
                ResetPath();
                Browse();
            }

            for (int i = 0; i < segments.Length; i++)
            {
                GUILayout.Label("\u25B8", GUILayout.Width(12));

                var isLast = i == segments.Length - 1;
                var style = new GUIStyle(EditorStyles.miniButton) { fontStyle = isLast ? FontStyle.Bold : FontStyle.Normal, stretchWidth = false };
                var displayName = segments[i];
                // Strip hash prefixes for display
                if (displayName.Contains('.'))
                    displayName = displayName.Substring(displayName.LastIndexOf('.') + 1);

                if (GUILayout.Button(displayName, style))
                {
                    // Navigate to this depth
                    path = string.Join("|", segments.Take(i + 1).Select(s => "|" + s)).TrimStart('|');
                    path = string.Join("", segments.Take(i + 1).Select(s => "|" + s));
                    Browse();
                }
            }

            // Search field fills remaining space
            memberSearchFilter = EditorGUILayout.TextField(memberSearchFilter);
            var searchIcon = EditorGUIUtility.IconContent("Search Icon");
            GUILayout.Label(new GUIContent(searchIcon.image, "Search members by name or value"), GUILayout.Width(18), GUILayout.Height(16));
            if (!string.IsNullOrEmpty(memberSearchFilter) && GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(18)))
                memberSearchFilter = "";

            EditorGUILayout.EndHorizontal();
        }

        private string[] GetPathSegments()
        {
            if (string.IsNullOrEmpty(path)) return Array.Empty<string>();
            return path.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }

        // ── Filter Bar ───────────────────────────────────────────

        private void DrawFilterBar()
        {
            var rect = EditorGUILayout.GetControlRect();

            var isVisual = _currentObject is Texture or Sprite or Material;
            var wPreview = isVisual ? 55f : 0f;
            var wCopy = 42f;
            var wFilter1 = 90f;
            var wFilter2 = 135f;
            var wRight = wCopy + wPreview + wFilter1 + wFilter2 + 8f;
            var w0 = 50f;
            var w1 = rect.width - w0 - wRight;

            var r0 = new Rect(rect.x, rect.y, w0, rect.height);
            var r1 = new Rect(rect.x + w0, rect.y, w1 - 4f, rect.height);

            var x = r1.xMax + 4f;

            if (isVisual)
            {
                var rPreview = new Rect(x, rect.y, wPreview, rect.height);
                if (GUI.Button(rPreview, new GUIContent("Preview", "Open preview window")))
                {
                    var previewObj = _currentObject as Object;
                    if (previewObj != null) PreviewWindow.Open(previewObj);
                }
                x += wPreview + 2f;
            }

            var rCopy = new Rect(x, rect.y, wCopy, rect.height);
            if (GUI.Button(rCopy, new GUIContent("Copy", "Copy current value to clipboard")))
                GUIUtility.systemCopyBuffer = ObjectReflectionExposer.ValueToString(_currentObject);
            x = rCopy.xMax + 2f;

            var r2 = new Rect(x, rect.y, wFilter1, rect.height);
            var r3 = new Rect(r2.xMax, rect.y, wFilter2, rect.height);

            GUI.Label(r0, "Current");
            DrawCurrentObject(r1);

            var mfType = (MemberFilterType)EditorGUI.EnumPopup(r2, memberFilterType);
            var rfType = (ReflectionFilterType)EditorGUI.EnumPopup(r3, reflectionFilterType);

            if (rfType != reflectionFilterType || mfType != memberFilterType)
            {
                reflectionFilterType = rfType;
                memberFilterType = mfType;
                Browse();
            }
        }

        // ── Items ────────────────────────────────────────────────

        private void DrawItems()
        {
            if (_displayItems == null || _displayItems.Count == 0) return;

            var allowExpose = _currentObject == null || !ObjectReflectionExposer.IsPrimitive(_currentObject.GetType());
            var hasObject = _currentReflectionType == null && _currentObject != null;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true));

            var items = string.IsNullOrEmpty(memberSearchFilter)
                ? _displayItems
                : _displayItems.Where(i =>
                    (i.DisplayMemberName != null &&
                        i.DisplayMemberName.IndexOf(memberSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (i.DisplayValue != null &&
                        i.DisplayValue.IndexOf(memberSearchFilter, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();

            ObjectExposedItemsDrawer.DrawExposedItems(
                items, OnItemClicked, allowExpose, hasObject, displayTypeHash, _currentObject);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEmptyState()
        {
            if (rootObject == null)
            {
                GUILayout.Box("Drag UnityEngine.Object into the above Object Field",
                    new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 25, wordWrap = true },
                    GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }
        }

        // ── Navigation ──────────────────────────────────────────

        private void NavigateHistory(int direction)
        {
            var newCursor = _historyCursor + direction;
            if (newCursor < 0 || newCursor >= _pathHistory.Count) return;

            _navigating = true;
            _historyCursor = newCursor;
            path = _pathHistory[_historyCursor];
            Browse();
            _navigating = false;
        }

        private void PushHistory()
        {
            if (_navigating) return;

            var currentPath = path ?? "";

            // Truncate forward history
            if (_historyCursor < _pathHistory.Count - 1)
                _pathHistory.RemoveRange(_historyCursor + 1, _pathHistory.Count - _historyCursor - 1);

            // Don't add duplicate
            if (_pathHistory.Count > 0 && _pathHistory[^1] == currentPath)
                return;

            _pathHistory.Add(currentPath);
            if (_pathHistory.Count > 100)
                _pathHistory.RemoveAt(0);

            _historyCursor = _pathHistory.Count - 1;
        }

        // ── Core ─────────────────────────────────────────────────

        private void PickInMemory()
        {
            var allObjects = Resources.FindObjectsOfTypeAll<Object>()
                .Where(o =>
                {
                    // Exclude assets and packages — only in-memory objects
                    var assetPath = AssetDatabase.GetAssetPath(o);
                    if (!string.IsNullOrEmpty(assetPath)) return false;
                    // Exclude hidden Unity internals
                    if (o.hideFlags.HasFlag(HideFlags.HideAndDontSave) && string.IsNullOrEmpty(o.name)) return false;
                    return true;
                });

            var dic = new Dictionary<string, Object>();
            foreach (var o in allObjects)
            {
                var typeName = o.GetType().Name;
                string label;

                if (o is EditorWindow ew)
                    label = $"[EditorWindow] {ew.titleContent.text} ({typeName}@{o.GetInstanceID()})";
                else if (o is GameObject go)
                    label = $"[GameObject] {go.name} ({typeName}@{o.GetInstanceID()})";
                else if (o is Component comp)
                    label = $"[Component] {comp.gameObject.name}/{typeName} (@{o.GetInstanceID()})";
                else
                    label = $"[{typeName}] {o.name} (@{o.GetInstanceID()})";

                dic.TryAdd(label, o);
            }

            SearchWindow.Show(dic.Keys, t => Browse(dic[t], ""));
        }

        public void Browse(Object ro, string path = "")
        {
            rootObject = ro;
            this.path = path;
            Browse();
        }

        public void Browse()
        {
            UpdateCurrentObject();
            Expose();
            PushHistory();
        }

        private void DrawCurrentObject(Rect rect)
        {
            var rect_Value = new Rect(rect);
            var rect_Type = new Rect(rect);

            rect_Value.width /= 3f;
            rect_Type.width = rect.width - rect_Value.width - 2;
            rect_Type.x += rect_Value.width + 2;

            var reflectionType = _currentObject is MonoScript ms ? ms.GetClass() : _currentObject?.GetType() ?? _currentReflectionType;
            var reflectionTypeName = reflectionType != null ? reflectionType.FullName : "";
            if (displayTypeHash && !string.IsNullOrEmpty(reflectionTypeName))
            {
                reflectionTypeName += $"[{BaseAndInterfacesHashResolver.GetShortHash(reflectionType.FullName)}]";
            }

            if (_currentObject is Object obj)
            {
                var enabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.ObjectField(rect_Value, obj, typeof(Object), true);
                GUI.enabled = enabled;
                EditorGUI.LabelField(rect_Type, reflectionTypeName);
            }
            else
            {
                var value = ObjectReflectionExposer.ValueToString(_currentObject);
                EditorGUI.TextField(rect_Value, value);
                EditorGUI.LabelField(rect_Type, reflectionTypeName);
            }
        }

        private static void DrawComponentSelectingButton(Object rootObject, Action<Object> selectedHandler)
        {
            var selectable = rootObject is GameObject or Component or ScriptableObject;
            if (!selectable) return;

            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                var menu = new GenericMenu();

                var objects = rootObject switch
                {
                    GameObject go => go.GetComponents<Component>().OfType<Object>().Append(go),
                    Component co => co.gameObject.GetComponents<Component>().OfType<Object>().Append(co.gameObject),
                    ScriptableObject so => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(so)),
                    _ => throw new ArgumentOutOfRangeException()
                };

                foreach (var i in objects)
                {
                    menu.AddItem(new GUIContent(i.GetType().Name), rootObject == i, () => selectedHandler?.Invoke(i));
                }

                menu.ShowAsContext();
            }
        }

        private void OnItemClicked(ObjectExposedItem item)
        {
            if (item.MemberInfo is MethodInfo methodInfo)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    methodInfo.Invoke(_currentObject, null);
                    Debug.Log($"Invoked method {methodInfo.Name} on object {_currentObject}");
                }
            }
            else
            {
                GoInto(item.MemberName);
            }
        }

        private void GoInto(string pathSegment)
        {
            AppendPath(pathSegment);
            Browse();
        }

        public void ChangeRootObject(Object rootObject)
        {
            this.rootObject = rootObject;
        }

        public void ResetPath()
        {
            path = "";
        }

        private void AppendPath(string memberName)
        {
            path = string.Concat(path, $"|{memberName}");
        }

        private void UpdateCurrentObject()
        {
            if (!string.IsNullOrEmpty(path))
            {
                var rootReflectionType = rootObject is MonoScript ms ? ms.GetClass() : null;
                var pathExecutor = new ReflectionPathExecutor(path, rootObject, rootReflectionType);
                _currentObject = pathExecutor.ExecutePath();
                _currentReflectionType = _currentObject != null ? _currentObject.GetType() : pathExecutor.GetFinalReflectionType();
            }
            else
            {
                var rootReflectionType = rootObject is MonoScript ms ? ms.GetClass() : null;
                _currentObject = rootObject;
                _currentReflectionType = rootReflectionType;
            }
        }

        private void Expose()
        {
            if (rootObject == null)
            {
                _displayItems = null;
                return;
            }

            _displayItems = ObjectReflectionExposer.ExposeObject(
                    _currentObject,
                    _currentReflectionType ?? _currentObject?.GetType(),
                    new ReflectionExtractor(reflectionFilterType, memberFilterType))
                .ToArray();

            OnExposed?.Invoke(this);
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Display Type Hash"), displayTypeHash, () =>
            {
                displayTypeHash = !displayTypeHash;
            });
        }
    }
}
#endif
