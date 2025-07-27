#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Snm.Tools.InspectorExtra;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sieunguoimay.Tools
{
    public class ObjectBrowserWindow : EditorWindow, RuntimeObjectExpose.ITargetObjectProvider
    {
        private const string RuntimeObjectBrowser = "Object Browser";

        private Object _rootObject;
        private string _path;
        private bool _refreshEveryFrame;

        private IReadOnlyList<RuntimeObjectExpose.ObjectExposedItem> _displayItems;
        private RuntimeObjectExpose.CommonRuntimeObjectExposeEditor _commonRuntimeObjectExposeEditor;

        private RuntimeObjectExpose _objectExpose;
        private RuntimeObjectExpose ObjectExpose => _objectExpose ??= new RuntimeObjectExpose(this);

        private RuntimeObjectExpose.CommonRuntimeObjectExposeEditor CommonRuntimeObjectExposeEditor
        {
            get { return _commonRuntimeObjectExposeEditor ??= new RuntimeObjectExpose.CommonRuntimeObjectExposeEditor(OnItemClicked); }
        }

        public object TargetObject { get; private set; }

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
                    UpdateCurrentObject();
                    Expose();
                });
            }

            GUILayout.Space(50);
            EditorGUILayout.EndHorizontal();

            if (_rootObject != rootObject)
            {
                ChangeRootObject(rootObject);
                ResetPath();
                UpdateCurrentObject();
                Expose();
            }


            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            var enableBackButton = !string.IsNullOrEmpty(_path);
            var ge = GUI.enabled;
            GUI.enabled = enableBackButton;
            if (GUILayout.Button("<-", GUILayout.Width(25)))
            {
                RemoveLastPathSegment();
                UpdateCurrentObject();
                Expose();
            }

            GUI.enabled = ge;
            _path = EditorGUILayout.TextField(_path);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                UpdateCurrentObject();
                Expose();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            DrawCurrentObjectType();
            GUILayout.FlexibleSpace();
            _refreshEveryFrame = GUILayout.Toggle(_refreshEveryFrame, "Refresh every frame");
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            if (_displayItems != null)
            {
                CommonRuntimeObjectExposeEditor.DrawExposedItems(_displayItems);
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

        private void DrawCurrentObjectType()
        {
            EditorGUILayout.LabelField($"{TargetObject}");
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

        private void OnItemClicked(RuntimeObjectExpose.ObjectExposedItem item)
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

        public void GoInto(RuntimeObjectExpose.ObjectExposedItem item)
        {
            GoInto(item.FieldName);
        }

        private void GoInto(string pathSegment)
        {
            AppendPath(pathSegment);
            UpdateCurrentObject();
            Expose();
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
            _path = string.Concat(_path, $".{memberName}");
        }

        private void RemoveLastPathSegment()
        {
            var lastIndexOf = _path.LastIndexOf(".", StringComparison.Ordinal);
            if (lastIndexOf >= 0)
            {
                _path = _path[..lastIndexOf];
            }
        }

        private void UpdateCurrentObject()
        {
            if (!string.IsNullOrEmpty(_path))
            {
                var pe = new UnityObjectPathSelector.PathExecutor();
                pe.Setup(_path, _rootObject, false, false, Application.isPlaying);
                TargetObject = pe.ExecutePath();
            }
            else
            {
                TargetObject = _rootObject;
            }

            if (TargetObject == null || _rootObject == null) return;
            ObjectExpose.UpdateReflectionInfos();
        }

        private void Expose()
        {
            if (TargetObject == null || _rootObject == null)
            {
                _displayItems = null;
                return;
            }

            _displayItems = ObjectExpose.ExposeObject();
        }
    }
}
#endif