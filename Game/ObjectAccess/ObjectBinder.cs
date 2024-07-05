#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ObjectAccess
{
    public class ObjectBinder : MonoBehaviour
    {
        [SerializeField] private RegistryEntrySelect bindTarget;
        [ObjectSelector]
        [SerializeField] private UnityEngine.Object obj;

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Bind()
        {
            bindTarget.Registry.Register(bindTarget.Entry, obj);
        }

        public void Unbind()
        {
            bindTarget.Registry.Unregister(bindTarget.Entry);
        }

#if UNITY_EDITOR
        [ContextMenu("CreateNewEntryForThisObject")]
        private void CreateNewEntryForThisObject()
        {
            if (obj != null && bindTarget.Registry != null)
            {
                bindTarget.Registry.AddEntry_Editor(obj.name, obj.GetType().AssemblyQualifiedName);
                using (var so = new UnityEditor.SerializedObject(this))
                {
                    so.FindProperty(nameof(bindTarget)).FindPropertyRelative("entryName").stringValue = obj.name;
                    so.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(bindTarget.Registry);
                AssetDatabase.SaveAssets();
            }
        }

        [ContextMenu("UpdateEntryTypeToThisObject")]
        private void UpdateEntryTypeToThisObject()
        {
            if (obj != null && bindTarget.Registry != null)
            {
                bindTarget.Entry.type = obj.GetType().AssemblyQualifiedName;
                AssetDatabase.SaveAssets();
            }
        }
#endif
    }

}