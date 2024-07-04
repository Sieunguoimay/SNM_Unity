#if UNITY_EDITOR
using UnityEditor;

namespace InspectorExtensions
{
    [InitializeOnLoad]
    public class InspectorExtensionEntryPoint
    {
        static InspectorExtensionEntryPoint()
        {
            InspectorExtensionInstaller.Instance.AddExtension(new MaterialInspectorExt());
            InspectorExtensionInstaller.Instance.AddExtension(new RevealNonSerializedExt());
            InspectorExtensionInstaller.Instance.AddExtension(new IMGUIMethodExt());
            InspectorExtensionInstaller.Instance.AddExtension(new ContextMenuInspectorExt());
            InspectorExtensionInstaller.Instance.AddExtension(new RevealReferenceEditorExt());
            InspectorExtensionInstaller.Instance.AddExtension(new ScriptableObjectInspectorExt());
            InspectorExtensionInstaller.Instance.AddExtension(new CreateVisualElementExt());
            InspectorExtensionInstaller.Instance.AddExtension(new AnimationClipInspectorExt());
            InspectorExtensionInstaller.Instance.Init();
        }
    }
}

#endif