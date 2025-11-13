#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private IReadOnlyList<ObjectExposedItem> _displayItems;
        private object _currentObject;
        private Type _currentReflectionType;
        private Vector2 _scrollPos;

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

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField(MonoScript.FromScriptableObject(this), GetType(), false);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50);
            var ro = EditorGUILayout.ObjectField("Source Object", rootObject, typeof(Object), true);
            DrawComponentSelectingButton(rootObject, i =>
            {
                ChangeRootObject(i);
                ResetPath();
                Browse();
            });

            if (GUILayout.Button(new GUIContent("@", "Pick In Memory Object"), GUILayout.Width(25)))
            {
                var supportedTypes = new Type[] {
                    typeof(ScriptableObject),
                    typeof(Material),
                    typeof(Mesh),
                    typeof(Texture)
                };
                var menu = new GenericMenu();
                foreach (var t in supportedTypes)
                {
                    var current = t;
                    menu.AddItem(new GUIContent(t.Name), false, () =>
                    {
                        PickInMemoryObject(current);
                    });
                }
                menu.ShowAsContext();
            }

            GUILayout.Space(50);
            EditorGUILayout.EndHorizontal();

            if (rootObject != ro)
            {
                ChangeRootObject(ro);
                ResetPath();
                Browse();
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            var enableBackButton = !string.IsNullOrEmpty(path);
            var ge = GUI.enabled;
            GUI.enabled = enableBackButton;
            if (GUILayout.Button("<-", GUILayout.Width(25)))
            {
                RemoveLastPathSegment();
                Browse();
            }

            GUI.enabled = ge;
            path = EditorGUILayout.TextField(path);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                Browse();
            }

            EditorGUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect();

            var w3 = 135f;
            var w2 = 90f;
            var w0 = 50f;
            var w1 = rect.width - w2 - w3 - w0;

            var r0 = new Rect(rect.x, rect.y, w0, rect.height);
            var r1 = new Rect(rect.x + w0, rect.y, w1 - 4f, rect.height);
            var r2 = new Rect(rect.x + w0 + w1, rect.y, w2, rect.height);
            var r3 = new Rect(rect.x + w0 + w1 + w2, rect.y, w3, rect.height);

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

            if (_displayItems != null && _displayItems.Count > 0)
            {
                var allowExpose = _currentObject == null || !ObjectReflectionExposer.IsPrimitive(_currentObject.GetType());
                var hasObject = _currentReflectionType == null && _currentObject != null;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.helpBox, GUILayout.ExpandWidth(true));

                ObjectExposedItemsDrawer.DrawExposedItems(_displayItems, OnItemClicked, allowExpose, hasObject, displayTypeHash);

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            if (rootObject == null)
            {
                GUILayout.Box("Drag UnityEngine.Object into the above Object Field", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 25, wordWrap = true }, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }
        }

        private void PickInMemoryObject(Type targetType)
        {
            var nonAssetScriptableObjects = Resources.FindObjectsOfTypeAll(targetType)
                .Where(o => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o)));
            var dic = nonAssetScriptableObjects
                .ToDictionary(o => $"{o.name} ({o.GetType().Name}@{o.GetInstanceID()})", o => o);

            SearchWindow.Show(dic.Keys, t => Browse(dic[t], ""));
        }

        public void Browse(UnityEngine.Object ro, string path)
        {
            rootObject = ro;
            this.path = path;
            Browse();
        }

        public void Browse()
        {
            UpdateCurrentObject();
            Expose();
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
                    GameObject go => go.GetComponents<Component>().OfType<UnityEngine.Object>().Append(go),
                    Component co => co.gameObject.GetComponents<Component>().OfType<UnityEngine.Object>().Append(co.gameObject),
                    ScriptableObject so => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(so)),
                    _ => throw new ArgumentOutOfRangeException()
                };

                foreach (var i in objects)
                {
                    menu.AddItem(new GUIContent(i.GetType().Name), rootObject == i, () => { selectedHandler?.Invoke(i); });
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

        private void RemoveLastPathSegment()
        {
            var lastIndexOf = path.LastIndexOf("|", StringComparison.Ordinal);
            if (lastIndexOf >= 0)
            {
                path = path[..lastIndexOf];
            }
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