using System.IO;
using Snm.Runtime.GPUSkinning.Serialize;
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneWeightTool
{
    public class AssetExportTool
    {
        public static void ExportToSkeletonAsset(Bone[] bones, ref SkeletonAsset skeletonAsset)
        {
            if (skeletonAsset == null)
            {
                skeletonAsset = ScriptableObject.CreateInstance<SkeletonAsset>();
                skeletonAsset.name = "SkeletonAsset";

                CreateAsset(skeletonAsset);
            }
            skeletonAsset.skeleton = new Skeleton { bones = bones };
            EditorUtility.SetDirty(skeletonAsset);
            AssetDatabase.SaveAssetIfDirty(skeletonAsset);
        }

        public static void ExportBoneWeightsToMesh(BoneWeight[] boneWeights, Mesh mesh, ref Mesh outputMesh)
        {
            if (outputMesh == null || !AssetDatabase.GetAssetPath(outputMesh).StartsWith("Assets/"))
            {
                outputMesh = UnityEngine.Object.Instantiate(mesh);
                outputMesh.name = mesh.name;

                CreateAsset(outputMesh);
            }
            outputMesh.boneWeights = boneWeights;
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }

        public static void ExportBindposesToMesh(Matrix4x4[] bindposes, Mesh mesh, ref Mesh outputMesh)
        {
            if (outputMesh == null || !AssetDatabase.GetAssetPath(outputMesh).StartsWith("Assets/"))
            {
                outputMesh = UnityEngine.Object.Instantiate(mesh);
                outputMesh.name = mesh.name;

                CreateAsset(outputMesh);
            }
            outputMesh.bindposes = bindposes;
            EditorUtility.SetDirty(outputMesh);
            AssetDatabase.SaveAssetIfDirty(outputMesh);
        }

        private static void CreateAsset(UnityEngine.Object asset)
        {
            var p = AssetDatabase.GetAssetPath(Selection.activeObject);
            p = p.StartsWith("Assets/") ? p : "Assets/";
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(p) + "/" + asset.name + ".asset"));
        }
    }

    // public class BoneToolLoader
    // {
    //     public static BoneTool CreateBoneWeightTool(
    //         BoneWeight[] boneWeights, 
    //         Skeleton skeleton, out Func<BoneWeight[]> exportFunc)
    //     {
    //         var tool = new BoneTool(new VerticesSelectionTool());

    //         var boneCount = skeleton.bones.Length;
    //         var bones = RuntimeBoneImporter.Import(BoneWeightConverter.ConvertToBoneDatas(boneWeights, boneCount));
    //         tool.Import(boneWeights, skeleton.bones);

    //         var transforms = tool.BoneTransformsTool.BoneTransforms.Select(bt => bt.transform).ToArray();
    //         BoneTransformsTool.ApplySkeletonPoses(transforms, skeleton.bones, Matrix4x4.identity);

    //         exportFunc = () => BoneWeightConverter.ExtractBoneWeights(RuntimeBoneImporter.Export(tool.Bones), boneWeights.Length);

    //         return tool;
    //     }
    // }
}