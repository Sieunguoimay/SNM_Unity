using UnityEngine;
using System.Linq;
using Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Snm.Framework.NodeHierarchy
{
    public partial class ChildNodeEntry
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ChildNodeEntry))]
        public class ChildNodeEntryDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var h = EditorGUIUtility.singleLineHeight;

                var childNode = property.FindPropertyRelative("childNode");
                var assigners = property.FindPropertyRelative("assigners");

                var r = new Rect(position.x, position.y, position.width - 50, h);
                EditorGUI.PropertyField(r, childNode);

                var rb = new Rect(position.x + position.width - 45, position.y, 25, h);
                if (GUI.Button(rb, new GUIContent("All"))) OnAddAllButtonClicked(assigners, childNode.objectReferenceValue);
                rb.x += 25;
                rb.width = 20;
                if (GUI.Button(rb, new GUIContent("+", "Add First"))) OnAddButtonClicked(assigners);
                // rb.x += 25;
                // if (GUI.Button(rb, new GUIContent("-", "Remove First"))) OnRemoveButtonClicked(assigners);

                var r2 = new Rect(position.x + 20, position.y + h + 2, position.width - 20 - 20, h);
                var c = GUI.color;
                for (var i = 0; i < assigners.arraySize; i++)
                {
                    EditorGUI.PropertyField(r2, assigners.GetArrayElementAtIndex(i), GUIContent.none, true);
                    var rbb = new Rect(r2.x + r2.width, r2.y, 20, r2.height);
                    GUI.color = Color.red;
                    if (GUI.Button(rbb, "x")) OnRemoveButtonClicked(assigners, i);
                    GUI.color = c;
                    r2.y += h + 2;
                }
            }

            private void OnAddAllButtonClicked(SerializedProperty assigners, object childNode)
            {
                var injectFields = ReflectiveFieldAssigner.GetDestMembers(childNode)
                    .Where(n =>
                    {
                        for (var i = 0; i < assigners.arraySize; i++)
                        {
                            if (assigners.GetArrayElementAtIndex(i).FindPropertyRelative("destMemberName").stringValue == n)
                                return false;
                        }
                        return true;
                    }).ToArray();

                if (injectFields.Length > 0)
                {
                    foreach (var injectField in injectFields)
                    {
                        assigners.InsertArrayElementAtIndex(0);
                        assigners.serializedObject.ApplyModifiedProperties();
                        assigners.GetArrayElementAtIndex(0).FindPropertyRelative("destMemberName").stringValue = injectField;
                        assigners.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            private void OnRemoveButtonClicked(SerializedProperty assigners, int index)
            {
                assigners.DeleteArrayElementAtIndex(index);
                assigners.serializedObject.ApplyModifiedProperties();
            }

            private void OnAddButtonClicked(SerializedProperty assigners)
            {
                assigners.InsertArrayElementAtIndex(0);
                assigners.serializedObject.ApplyModifiedProperties();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var assigners = property.FindPropertyRelative("assigners");

                return (EditorGUIUtility.singleLineHeight + 2) * (1 + assigners.arraySize);
            }
        }
#endif
    }

}

