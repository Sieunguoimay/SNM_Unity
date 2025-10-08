using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Snm.Tools
{
    public class GameSceneDebugToolMB : MonoBehaviour
    {
        private ContextMenuEntry[] _contextMenuEntries;
        private bool _showingMenu;

        private void Start()
        {
            _contextMenuEntries = FindAllInScene().ToArray();
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                _showingMenu = !_showingMenu;
            }

            if (_showingMenu)
            {
                if (GUILayout.Button("Refresh"))
                {
                    _contextMenuEntries = FindAllInScene().ToArray();
                }

                foreach (var entry in _contextMenuEntries)
                {
                    if (entry.Target != null)
                    {
                        if (GUILayout.Button($"{entry.MenuName}"))
                        {
                            entry.Invoke();
                        }
                    }
                }
            }

            GUILayout.EndArea();

            if (DetectClickOutside())
            {
                _showingMenu = false;
            }
        }

        private bool DetectClickOutside()
        {
            // Then check the event
            if (Event.current.rawType == EventType.MouseDown && Event.current.button == 0)
            {
                // If some control consumed the click it will have converted type -> EventType.Used
                if (Event.current.type != EventType.Used)
                {
                    Debug.Log("Clicked outside all GUI controls");
                    Event.current.Use(); // optional: consume the event now
                    return true;
                }
                else
                {
                    // consumed by a control
                }
            }
            return false;
        }

        public static IEnumerable<ContextMenuEntry> FindAllInScene()
        {
            var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var behaviour in allBehaviours)
            {
                var type = behaviour.GetType();
                while (type != null && typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    var methods = type.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<ContextMenu>();
                        if (attr != null)
                        {
                            yield return new ContextMenuEntry
                            {
                                Target = behaviour,
                                Method = method,
                                MenuName = attr.menuItem
                            };
                        }
                    }
                    type = type.BaseType;
                }
            }
        }

        public struct ContextMenuEntry
        {
            public UnityEngine.Object Target;
            public MethodInfo Method;
            public string MenuName;

            public void Invoke()
            {
                Method.Invoke(Target, null);
            }
        }
    }
}