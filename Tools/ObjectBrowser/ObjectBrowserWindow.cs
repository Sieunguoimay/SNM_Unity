#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sieunguoimay.Tools
{
    public class ObjectBrowserWindow : EditorWindow, RuntimeObjectExpose.ITargetObjectProvider
    {
        private const string RuntimeObjectBrowser = "Object Browser";

        [SerializeField] private Object _rootObject;
        [SerializeField] private string _path;
        [SerializeField] private bool _refreshEveryFrame;

        private IReadOnlyList<ObjectExposedItem> _displayItems;
        private ObjectExposedItemsDrawer _commonRuntimeObjectExposeEditor;

        private RuntimeObjectExpose _objectExpose;
        private RuntimeObjectExpose ObjectExpose => _objectExpose ??= new RuntimeObjectExpose(this);

        private ObjectExposedItemsDrawer CommonRuntimeObjectExposeEditor => _commonRuntimeObjectExposeEditor ??= new ObjectExposedItemsDrawer(OnItemClicked);

        public object TargetObject { get; private set; }
        public string Path { get => _path; set => _path = value; }
        public Object RootObject { get => _rootObject; set => _rootObject = value; }

        public event Action<ObjectBrowserWindow> OnExposed;
        public event Action<ObjectBrowserWindow> OnClosed;

        [MenuItem("Tools/Snm/Object Browser")]
        public static void OpenWindow()
        {
            GetWindow(typeof(ObjectBrowserWindow), false, RuntimeObjectBrowser).Show();
        }

        public static ObjectBrowserWindow OpenWindowAndReturnSelf()
        {
            var window = GetWindow(typeof(ObjectBrowserWindow), false, RuntimeObjectBrowser);
            window.Show();
            return window as ObjectBrowserWindow;
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
            var rootObject = EditorGUILayout.ObjectField("Source Object", _rootObject, typeof(Object), true);
            var canGetComponents = _rootObject is GameObject or Component;
            if (canGetComponents)
            {
                DrawComponentSelectingButton(_rootObject, i =>
                {
                    ChangeRootObject(i);
                    ResetPath();
                    Browse();
                });
            }

            GUILayout.Space(50);
            EditorGUILayout.EndHorizontal();

            if (_rootObject != rootObject)
            {
                ChangeRootObject(rootObject);
                ResetPath();
                Browse();
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            var enableBackButton = !string.IsNullOrEmpty(_path);
            var ge = GUI.enabled;
            GUI.enabled = enableBackButton;
            if (GUILayout.Button("<-", GUILayout.Width(25)))
            {
                RemoveLastPathSegment();
                Browse();
            }

            GUI.enabled = ge;
            _path = EditorGUILayout.TextField(_path);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                Browse();
            }

            EditorGUILayout.EndHorizontal();

            var rect = EditorGUILayout.GetControlRect();
            var leftRect = new Rect(rect.x, rect.y, rect.width - 130f - 4f, rect.height);
            DrawCurrentObject(leftRect);
            _refreshEveryFrame = GUI.Toggle(new Rect(rect.x + rect.width - 130f, rect.y, 130f, rect.height), _refreshEveryFrame, "Refresh every frame");

            if (_displayItems != null && _displayItems.Count > 0)
            {
                CommonRuntimeObjectExposeEditor.DrawExposedItems(_displayItems, !RuntimeObjectExpose.IsPrimitive(TargetObject.GetType()));
            }

            if (_rootObject == null)
            {
                GUILayout.Box("Drag UnityEngine.Object into the above Object Field", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 25, wordWrap = true }, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            }

            if (_refreshEveryFrame)
            {
                Expose();
            }
        }

        public void Browse()
        {
            UpdateCurrentObject();
            Expose();
        }

        private void DrawCurrentObject(Rect leftRect)
        {
            if (TargetObject is UnityEngine.Object obj)
            {
                EditorGUI.ObjectField(leftRect, obj, typeof(UnityEngine.Object), true);
            }
            else
            {
                EditorGUI.LabelField(leftRect, $"{TargetObject}");
            }
        }

        private static void DrawComponentSelectingButton(Object rootObject, Action<Object> selectedHandler)
        {
            if (!GUILayout.Button("...", GUILayout.Width(20))) return;

            var menu = new GenericMenu();
            var gameObject = rootObject switch
            {
                GameObject go => go,
                Component co => co.gameObject,
                _ => throw new ArgumentOutOfRangeException()
            };
            IEnumerable<Object> interfaces = gameObject.GetComponents<Component>();
            interfaces = interfaces.Append(gameObject);

            foreach (var i in interfaces)
            {
                menu.AddItem(new GUIContent(i.GetType().Name), rootObject == i, () => { selectedHandler?.Invoke(i); });
            }

            menu.ShowAsContext();
        }

        private void OnItemClicked(ObjectExposedItem item)
        {
            if (item.MemberInfo is MethodInfo methodInfo)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    methodInfo.Invoke(TargetObject, null);
                    Debug.Log($"Invoked method {methodInfo.Name} on object {TargetObject}");
                }
            }
            else
            {
                GoInto(item);
            }
        }

        public void GoInto(ObjectExposedItem item)
        {
            GoInto(item.MemberName);
        }

        private void GoInto(string pathSegment)
        {
            AppendPath(pathSegment);
            Browse();
        }

        public void ChangeRootObject(Object rootObject)
        {
            _rootObject = rootObject;
        }

        public void ResetPath()
        {
            _path = "";
        }

        private void AppendPath(string memberName)
        {
            _path = string.Concat(_path, $"|{memberName}");
        }

        private void RemoveLastPathSegment()
        {
            var lastIndexOf = _path.LastIndexOf("|", StringComparison.Ordinal);
            if (lastIndexOf >= 0)
            {
                _path = _path[..lastIndexOf];
            }
        }

        private void UpdateCurrentObject()
        {
            if (!string.IsNullOrEmpty(_path))
            {
                var pe = new ReflectionPathExecutor();
                pe.Setup(_path, _rootObject);
                TargetObject = pe.ExecutePath();
            }
            else
            {
                TargetObject = _rootObject;
            }

            // if (TargetObject == null || _rootObject == null) return;
            ObjectExpose.UpdateReflectionInfos();
        }

        private void Expose()
        {
            if (_rootObject == null)
            {
                _displayItems = null;
                return;
            }

            _displayItems = ObjectExpose.ExposeObject();

            OnExposed?.Invoke(this);
        }
    }
}
#endif