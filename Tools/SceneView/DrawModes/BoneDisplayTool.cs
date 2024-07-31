using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Bone Display", typeof(SkinnedMeshRenderer))]

public class BoneDisplayTool : EditorTool
{
    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView) return;
        foreach (var target in targets)
        {
            if (target is SkinnedMeshRenderer smr)
            {
                DrawBones(smr);
            }
        }
    }

    private void DrawBones(SkinnedMeshRenderer smr)
    {
        foreach (var b in smr.bones)
        {
            // Handles.DotHandleCap(100, b.position, b.rotation, 10, EventType.MouseDown);
        }
    }
}
