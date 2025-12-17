using System;
using System.IO;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneToolUICreator
    {
        public static void ExportSkinnedMesh(SerializeBone[] bones, Mesh mesh, ref Mesh outputMesh)
        {
            if (outputMesh == null)
            {
                outputMesh = UnityEngine.Object.Instantiate(mesh);
                var p = AssetDatabase.GetAssetPath(mesh);
                p = p.StartsWith("Assets/") ? p : "Assets/";
                AssetDatabase.CreateAsset(outputMesh, AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(p) + "/" + mesh.name + ".asset"));
            }
            outputMesh.boneWeights = BoneWeightConverter.ConvertToBoneWeights(bones, outputMesh.vertices.Length);
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }

        public static BoneToolUI CreateToolUI(Mesh mesh, out Func<SerializeBone[]> export)
        {
            var bones = BoneWeightConverter.ConvertToBoneDatas(mesh.boneWeights);
            var runtimeBoneCollection = new RuntimeBoneCollection();
            var runtimeBoneImporter = new RuntimeBoneImporter(runtimeBoneCollection);
            runtimeBoneImporter.Import(bones);
            var toolUI = new BoneToolUI(runtimeBoneCollection, mesh);
            export = () => runtimeBoneImporter.Export();
            return toolUI;
        }
    }
}