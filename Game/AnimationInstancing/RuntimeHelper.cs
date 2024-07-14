using System.Collections.Generic;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public class RuntimeHelper
    {
        public static void MergeBone(SkinnedMeshRenderer[] skinnedMeshRenderers,
            out List<Transform> boneList,
            out List<Matrix4x4> bindPoseList)
        {
            UnityEngine.Profiling.Profiler.BeginSample("MergeBone()");
            bindPoseList = new List<Matrix4x4>(150);
            boneList = new List<Transform>(150);
            for (int i = 0; i != skinnedMeshRenderers.Length; ++i)
            {
                if (skinnedMeshRenderers[i] == null || skinnedMeshRenderers[i].sharedMesh == null) continue;

                var bones = skinnedMeshRenderers[i].bones;
                var checkBindPose = skinnedMeshRenderers[i].sharedMesh.bindposes;
                for (int j = 0; j != bones.Length; ++j)
                {
#if UNITY_EDITOR
                    Debug.Assert(checkBindPose[j].determinant != 0, "The bind pose can't be 0 matrix.");
#endif
                    // the bind pose is correct base on the skinnedMeshRenderer, so we need to replace it
                    int index = boneList.FindIndex(q => q == bones[j]);
                    if (index < 0)
                    {
                        boneList.Add(bones[j]);
                        bindPoseList.Add(checkBindPose[j]);
                    }
                    else
                    {
                        bindPoseList[index] = checkBindPose[j];
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public static Quaternion QuaternionFromMatrix(Matrix4x4 mat)
        {
            Vector3 forward;
            forward.x = mat.m02;
            forward.y = mat.m12;
            forward.z = mat.m22;

            Vector3 upwards;
            upwards.x = mat.m01;
            upwards.y = mat.m11;
            upwards.z = mat.m21;

            return Quaternion.LookRotation(forward, upwards);
        }
    }
}