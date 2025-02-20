#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SNMTools
{

    public class CreateGameObjectWithComponent : AdvancedDropdown
    {
        public GameObject context;
        public CreateGameObjectWithComponent(AdvancedDropdownState state, GameObject context) : base(state)
        {
            minimumSize = new Vector2(200, 200);
            this.context = context;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Component");
            var scripts = new AdvancedDropdownItem("Scripts");
            var buitins = new AdvancedDropdownItem("Built-ins");
            root.AddChild(scripts);
            root.AddChild(buitins);
            foreach (var c in GetAllMonoScripts())
            {
                scripts.AddChild(new Item(c.name, c.GetClass())
                {
                    icon = (Texture2D)EditorGUIUtility.ObjectContent(c, null).image,
                });
            }
            foreach (var c in GetAllBuiltInScripts())
            {
                buitins.AddChild(new Item(c.Name, c)
                {
                    icon = (Texture2D)EditorGUIUtility.ObjectContent(null, c).image,
                });
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var type = (item as Item).Type;
            var empty = new GameObject(item.name, type);

            if (context != null)
            {
                var parent = context.transform;
                empty.transform.SetParent(parent);
                empty.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            Debug.Log($"Created GameObject with component {item.name}", empty);
            Selection.activeGameObject = empty;
        }

        private IEnumerable<MonoScript> GetAllMonoScripts()
        {
            var mb = typeof(MonoBehaviour);
            var c = typeof(Component);
            return AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>).Where(ms =>
                {
                    var clss = ms.GetClass();
                    return clss != null
                        && clss.Name == ms.name
                        && (clss.IsSubclassOf(mb) || clss.IsSubclassOf(c))
                        && !clss.IsAbstract;
                });
        }
        private IEnumerable<Type> GetAllBuiltInScripts()
        {
            var c = typeof(Component);
            var mb = typeof(MonoBehaviour);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();

                var componentSubtypes = types.Where(t => t.IsSubclassOf(c) && !t.IsSubclassOf(mb));
                foreach (var t in componentSubtypes)
                {
                    yield return t;
                }
            }
        }

        [MenuItem("GameObject/Create With Component", priority = 1)]
        private static void CreateWithComponent(MenuCommand menuCommand)
        {
            new CreateGameObjectWithComponent(new AdvancedDropdownState(), menuCommand.context as GameObject)
                .Show(new Rect(30, 55, 0, 0));
        }

        private class Item : AdvancedDropdownItem
        {
            public Type Type { get; }
            public Item(string name, Type type) : base(name)
            {
                Type = type;
            }
        }
    }
}
#endif