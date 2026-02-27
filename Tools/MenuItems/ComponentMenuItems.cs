#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Tools.MenuItemExtra
{
    public static class ComponentMenuItems
    {
        [MenuItem("CONTEXT/Component/Snm/MoveToNewGameObject - Ignore dependents")]
        private static void MoveToNewGameObject(MenuCommand command)
        {
            CopyToNewGameObject(command);
            if (command.context is Component c)
            {
                Object.DestroyImmediate(c);
            }
        }

        [MenuItem("CONTEXT/Component/Snm/CloneToNewGameObject")]
        private static void CopyToNewGameObject(MenuCommand command)
        {
            if (command.context is Component c)
            {
                var source = new SerializedObject(c);
                source.Update();

                var go = new GameObject(c.GetType().Name);
                go.transform.SetParent(c.transform.parent);
                var newComponent = go.AddComponent(c.GetType());
                var target = new SerializedObject(newComponent);
                target.Update();

                var it = source.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyPath.StartsWith("m_GameObject")) continue;
                    target.CopyFromSerializedProperty(it);
                }
                target.ApplyModifiedProperties();
            }
        }
    }
}
#endif