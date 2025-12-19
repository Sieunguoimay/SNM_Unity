using System;
using System.IO;
using System.Linq;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class AssetExportTool
    {
        public static void ExportBoneHierarchy(int[] parents, string name, ref BoneHierarchyAsset hierarchyAsset)
        {
            if (hierarchyAsset == null)
            {
                hierarchyAsset = ScriptableObject.CreateInstance<BoneHierarchyAsset>();
                hierarchyAsset.name = name;
                CreateAsset(hierarchyAsset);
            }
            hierarchyAsset.boneHierarchy.parentIndices = parents;
            EditorUtility.SetDirty(hierarchyAsset);
            AssetDatabase.SaveAssetIfDirty(hierarchyAsset);
        }

        public static void ExportBoneDataAsSkinnedMesh(SerializeBone[] bones, Mesh mesh, ref Mesh outputMesh)
        {
            if (outputMesh == null)
            {
                outputMesh = UnityEngine.Object.Instantiate(mesh);
                outputMesh.name = mesh.name;

                CreateAsset(outputMesh);
            }
            outputMesh.bindposes = bones.Select(b => b.bindpose).ToArray();
            outputMesh.boneWeights = BoneWeightConverter.ExtractBoneWeights(bones, outputMesh.vertices.Length);
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }

        private static void CreateAsset(UnityEngine.Object outputMesh)
        {
            var p = AssetDatabase.GetAssetPath(Selection.activeObject);
            p = p.StartsWith("Assets/") ? p : "Assets/";
            AssetDatabase.CreateAsset(outputMesh, AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(p) + "/" + outputMesh.name + ".asset"));
        }
    }
    public class BoneToolCreator
    {

        public static void CreateTool(Mesh inputMesh, out BoneTool outTool)
        {
            var serializeBones = BoneWeightConverter.ConvertToBoneDatas(inputMesh.boneWeights, inputMesh.bindposes);
            var bones = RuntimeBoneImporter.Import(serializeBones);

            var verticesSelector = new VerticesSelectionTool(inputMesh.vertices);
            var tool = new BoneTool(bones, verticesSelector);

            outTool = tool;
        }
    }
}