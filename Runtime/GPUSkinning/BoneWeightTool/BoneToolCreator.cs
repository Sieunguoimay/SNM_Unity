using System;
using System.IO;
using System.Linq;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class BoneToolCreator
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
            outputMesh.bindposes = bones.Select(b => b.bindpose).ToArray();
            outputMesh.boneWeights = BoneWeightConverter.ExtractBoneWeights(bones, outputMesh.vertices.Length);
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }

        public static void CreateTool(Mesh mesh, out BoneTool outTool, out Func<SerializeBone[]> export)
        {
            var bones = BoneWeightConverter.ConvertToBoneDatas(mesh.boneWeights, mesh.bindposes);
            var runtimeBoneCollection = new RuntimeBoneCollection();

            RuntimeBoneImporter.Import(bones, runtimeBoneCollection);

            var verticesSelector = new VerticesSelectionTool(mesh.vertices);
            var tool = new BoneTool(runtimeBoneCollection, verticesSelector);

            SerializeBone[] Export()
            {
                tool.UpdateBindposes();
                return RuntimeBoneImporter.Export(runtimeBoneCollection);
            }

            outTool = tool;
            export = Export;
        }
    }
}