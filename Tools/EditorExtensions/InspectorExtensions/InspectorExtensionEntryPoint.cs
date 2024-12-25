#if UNITY_EDITOR
using UnityEditor;

namespace InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionEntryPoint
    {
        static InspectorExtensionEntryPoint()
        {
            // InspectorExtensionInstaller.Instance.Teardown();

            InspectorExtensionInstaller.Instance.InjectExtensions(
                // new MaterialInspectorExt(),
                new RevealNonSerializedExt(),
                new IMGUIMethodExt(),
                new ContextMenuInspectorExt(),
                // new RevealReferenceEditorExt(),
                new ScriptableObjectInspectorExt(),
                new CreateVisualElementExt(),    
                new EditorSecondHeaderExt()
                // new AnimationClipInspectorExt()
            );

            InspectorExtensionInstaller.Instance.Setup();

        }
    }
}
#endif