using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneAssetAndMeshToolWindow : EditorWindow
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private BoneAsset boneAsset;
        [SerializeField] private Mesh outputMesh;

        private Editor _editor;

        [MenuItem("CONTEXT/" + nameof(BoneAsset) + "/" + nameof(BoneAssetAndMeshToolWindow))]
        private static void OpenWindow(MenuCommand command)
        {
            var window = GetWindow<BoneAssetAndMeshToolWindow>();
            window.boneAsset = command.context as BoneAsset;
        }

        private void OnGUI()
        {
            _editor ??= Editor.CreateEditor(this);
            _editor.OnInspectorGUI();

            if (GUILayout.Button("Export Bone from Mesh to BoneAsset")) { MeshToBoneAsset(); }
            if (GUILayout.Button("Export As Skinned Mesh")) { ExportNewMesh(); }
        }

        private void MeshToBoneAsset()
        {
            var bones = BoneWeightConverter.ConvertToBoneDatas(mesh.boneWeights);
            if (boneAsset == null)
            {
                boneAsset = CreateInstance<BoneAsset>();
                AssetDatabase.CreateAsset(boneAsset, AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(mesh)) + "/" + mesh.name + ".asset"));
            }

            boneAsset.bones = bones;
            EditorUtility.SetDirty(boneAsset);
            AssetDatabase.SaveAssetIfDirty(boneAsset);
        }

        private void ExportNewMesh()
        {
            if (boneAsset == null || mesh == null) return;
            if (outputMesh == null)
            {
                outputMesh = Instantiate(mesh);
                var path = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(AssetDatabase.GetAssetPath(boneAsset)) + "/" + mesh.name + ".asset");
                AssetDatabase.CreateAsset(outputMesh, path);
            }
            outputMesh.boneWeights = BoneWeightConverter.ConvertToBoneWeights(boneAsset.bones, mesh.vertices.Length);
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }
    }
}