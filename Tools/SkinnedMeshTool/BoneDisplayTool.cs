#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Bone Display", typeof(SkinnedMeshRenderer))]
public class BoneDisplayTool : EditorTool
{
    static private Texture CircleTexture => EditorGUIUtility.FindTexture("sv_icon_dot0_pix16_gizmo");
    static private Material _boneWeightMaterial;
    static private Material BoneWeightMaterial
    {
        get
        {
            if (_boneWeightMaterial == null)
                _boneWeightMaterial = new Material(Shader.Find("Hidden/SkinnedMeshTools/VertexColorShader"));
            return _boneWeightMaterial;
        }
    }

    private Transform _selectedBone;
    private readonly ToolConfig toolConfig = new();

    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView) return;
        foreach (var target in targets)
        {
            if (target is SkinnedMeshRenderer smr)
            {
                if (_selectedBone != null && toolConfig.drawBoneWeight)
                {
                    DrawBoneWeights(Array.IndexOf(smr.bones, _selectedBone), smr);
                }

                DrawBones(smr);
            }
        }
        DrawToolConfig();

        if (_selectedBone != null)
        {
            if (toolConfig.useRotationHandle)
            {
                DrawBoneRotationHandle();
            }
            else
            {
                DrawBonePositionHandle();
            }
        }
    }

    private void DrawToolConfig()
    {
        Handles.BeginGUI();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(100));

        if (GUILayout.Button("Reset Pose"))
        {
            ResetPose();
        }

        if (_selectedBone != null)
        {
            GUILayout.Label("Selected: " + _selectedBone.name);

            var newValue = GUILayout.Toggle(toolConfig.useRotationHandle, "Rotate");
            if (newValue != toolConfig.useRotationHandle)
            {
                toolConfig.useRotationHandle = newValue;
            }

            newValue = GUILayout.Toggle(toolConfig.drawBoneWeight, "Bone weight");
            if (newValue != toolConfig.drawBoneWeight)
            {
                toolConfig.drawBoneWeight = newValue;
            }
            if (toolConfig.drawBoneWeight)
            {
                newValue = GUILayout.Toggle(toolConfig.transparentBoneWeight, "Transparent");
                if (newValue != toolConfig.transparentBoneWeight)
                {
                    toolConfig.transparentBoneWeight = newValue;
                }

                if (!toolConfig.transparentBoneWeight)
                {
                    EditorGUILayout.BeginHorizontal();
                    var newColor = EditorGUILayout.ColorField(toolConfig.boneWeightColor, GUILayout.ExpandWidth(true));
                    if (newColor != toolConfig.boneWeightColor)
                    {
                        toolConfig.boneWeightColor = newColor;
                    }
                    newColor = EditorGUILayout.ColorField(toolConfig.inversedBoneWeightColor, GUILayout.ExpandWidth(true));
                    if (newColor != toolConfig.inversedBoneWeightColor)
                    {
                        toolConfig.inversedBoneWeightColor = newColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndVertical();
        Handles.EndGUI();
    }

    private void ResetPose()
    {
        foreach (var target in targets)
        {
            if (target is SkinnedMeshRenderer smr)
            {
                var bindposes = smr.sharedMesh.bindposes;
                var bones = smr.bones;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (i < bindposes.Length)
                    {
                        bones[i].localPosition = bindposes[i].MultiplyPoint(Vector3.zero);
                        bones[i].localRotation = bindposes[i].rotation;
                        bones[i].localScale = bindposes[i].lossyScale;
                    }
                }
            }
        }
    }

    private void DrawBones(SkinnedMeshRenderer smr)
    {
        Transform prevBone = null;
        foreach (var b in smr.bones)
        {
            Handles.BeginGUI();

            var pos2D = HandleUtility.WorldToGUIPoint(b.position);
            var scale = 15;
            var rect = new Rect(pos2D.x - scale / 2, pos2D.y - scale / 2, scale, scale);

            using (new ColorScope(_selectedBone == b ? Color.green : GUI.color))
            {

                if (GUI.Button(rect, CircleTexture, GUIStyle.none))
                {
                    Debug.Log("Selected " + b.name);
                    if (_selectedBone == b)
                    {
                        _selectedBone = null;
                    }
                    else
                    {
                        _selectedBone = b;
                    }
                }
            }

            Handles.EndGUI();

            if (prevBone != null)
            {
                Handles.DrawLine(prevBone.position, b.position, 0);
            }

            prevBone = b;
        }
    }

    private void DrawBonePositionHandle()
    {
        using var check = new EditorGUI.ChangeCheckScope();
        var newPos = Handles.PositionHandle(_selectedBone.position, _selectedBone.rotation);
        if (check.changed)
        {
            Undo.RecordObject(_selectedBone, "Move Transform");
            _selectedBone.position = newPos;
        }
    }

    private void DrawBoneRotationHandle()
    {
        using var check = new EditorGUI.ChangeCheckScope();
        var newRot = Handles.RotationHandle(_selectedBone.rotation, _selectedBone.position);
        if (check.changed)
        {
            Undo.RecordObject(_selectedBone, "Rotate Transform");
            _selectedBone.rotation = newRot;
        }
    }

    private void DrawBoneWeights(int p_boneIndex, SkinnedMeshRenderer smr)
    {
        GL.Clear(true, false, Color.black);
        Mesh mesh = GenerateBoneWeightMesh(smr, p_boneIndex);
        if (BoneWeightMaterial != null)
        {
            BoneWeightMaterial.SetPass(0);
            Graphics.DrawMeshNow(mesh, smr.transform.localToWorldMatrix);
        }
    }

    private Mesh GenerateBoneWeightMesh(SkinnedMeshRenderer p_skinnedMesh, int p_boneIndex)
    {
        var mesh = new Mesh();
        p_skinnedMesh.BakeMesh(mesh);
        var colors = new Color[mesh.vertexCount];
        var boneWeights = p_skinnedMesh.sharedMesh.boneWeights;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            colors[i] = GetBoneWeightColor(boneWeights[i], p_boneIndex);
        }

        mesh.colors = colors;
        return mesh;
    }

    private Color GetBoneWeightColor(BoneWeight p_boneWeight, int p_boneIndex)
    {
        var boneWeightColor = toolConfig.boneWeightColor;

        if (toolConfig.transparentBoneWeight)
        {
            if (p_boneWeight.boneIndex0 == p_boneIndex)
                return new Color(boneWeightColor.r, boneWeightColor.g, boneWeightColor.b,
                    p_boneWeight.weight0);
            if (p_boneWeight.boneIndex1 == p_boneIndex)
                return new Color(boneWeightColor.r, boneWeightColor.g, boneWeightColor.b,
                    p_boneWeight.weight1);
            if (p_boneWeight.boneIndex2 == p_boneIndex)
                return new Color(boneWeightColor.r, boneWeightColor.g, boneWeightColor.b,
                    p_boneWeight.weight2);
            if (p_boneWeight.boneIndex3 == p_boneIndex)
                return new Color(boneWeightColor.r, boneWeightColor.g, boneWeightColor.b,
                    p_boneWeight.weight3);

            return new Color(0, 0, 0, 0);
        }

        if (p_boneWeight.boneIndex0 == p_boneIndex)
            return new Color(boneWeightColor.r * p_boneWeight.weight0,
                boneWeightColor.g * p_boneWeight.weight0, boneWeightColor.b * p_boneWeight.weight0, 1);
        if (p_boneWeight.boneIndex1 == p_boneIndex)
            return new Color(boneWeightColor.r * p_boneWeight.weight1,
                boneWeightColor.g * p_boneWeight.weight1, boneWeightColor.b * p_boneWeight.weight1, 1);
        if (p_boneWeight.boneIndex2 == p_boneIndex)
            return new Color(boneWeightColor.r * p_boneWeight.weight2,
                boneWeightColor.g * p_boneWeight.weight2, boneWeightColor.b * p_boneWeight.weight2, 1);
        if (p_boneWeight.boneIndex3 == p_boneIndex)
            return new Color(boneWeightColor.r * p_boneWeight.weight3,
                boneWeightColor.g * p_boneWeight.weight3, boneWeightColor.b * p_boneWeight.weight3, 1);

        return toolConfig.inversedBoneWeightColor;
    }

    public class ColorScope : IDisposable
    {
        private readonly Color oldColor;

        public ColorScope(Color color)
        {
            oldColor = GUI.color;
            GUI.color = color;
        }

        public void Dispose()
        {
            GUI.color = oldColor;
        }
    }

    private class ToolConfig
    {
        public Color boneWeightColor = new(1, 0, 0, 1);
        public Color inversedBoneWeightColor = new(0, 0, 1, 1);
        public bool drawBoneWeight = true;
        public bool transparentBoneWeight = true;
        public bool useRotationHandle = true;
    }
}
#endif