#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SNM_Unity.AnimationInstancing
{
    public interface IAnimationBakeData
    {
        GameObject Prefab { get; }
        List<string> SelectedExtraBones { get; }
        List<string> SelectedAnims { get; }
        int Fps { get; }
        AnimationInstancingData Asset { get; }
    }

    public class AnimationBaker
    {
        public static AnimationInstancingData BakeWithAnimator(IAnimationBakeData data)
        {
            return BakeWithAnimator(data.Prefab, data.SelectedExtraBones, data.SelectedAnims, data.Fps);
        }

        public static AnimationInstancingData BakeWithAnimator(
            GameObject prefab,
            List<string> selectedExtraBones,
            List<string> selectedAnims,
            int fps)
        {
            if (prefab == null)
            {
                return null;
            }

            var go = Object.Instantiate(prefab);
            go.name = prefab.name;
            go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var preGo = UnityEditor.Selection.activeGameObject;
            UnityEditor.Selection.activeGameObject = go;

            CreateBoneBakeInfos(go, selectedExtraBones,
                out var boneList,
                out var bindPoseList,
                out var extraBoneInfo);
            CreateAnimationBakeInfos(go, selectedAnims, fps,
                out var animBakeInfoList,
                out var cacheTransitions,
                out var cacheAnimationEvents);
            GenerateAnimationPoseData(animBakeInfoList, boneList, bindPoseList,
                out var animInfoList,
                out var animPoseDataList);
            ResetAnimationController(cacheTransitions, cacheAnimationEvents);

            var animationTextureData = AnimationTextureBaker
                .GenerateAnimationTextureData(animInfoList, animPoseDataList, boneList.Length);

            var animationData = ScriptableObject.CreateInstance<AnimationInstancingData>();
            animationData.animInfoList = animInfoList;
            animationData.boneData = extraBoneInfo;
            animationData.animationTextureData = animationTextureData;

            Object.DestroyImmediate(go);
            UnityEditor.Selection.activeGameObject = preGo;

            return animationData;
        }

        private static void CreateBoneBakeInfos(
            GameObject go,
            List<string> selectedExtraBones,
            out Transform[] allBones,
            out Matrix4x4[] allBindPoses,
            out ExtraBoneData extraBoneInfo)
        {
            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();

            RuntimeHelper.MergeBone(skinnedMeshRenderers, out var mainBones, out var mainBindPoses);

            GetExtraBones(go, selectedExtraBones, out var extraBones, out var extraBindPoses);

            allBones = mainBones.Concat(extraBones).ToArray();
            allBindPoses = mainBindPoses.Concat(extraBindPoses).ToArray();

            extraBoneInfo = new ExtraBoneData
            {
                extraBones = selectedExtraBones.ToArray(),
                extraBindPoses = extraBindPoses.ToArray()
            };
        }


        private static void CreateAnimationBakeInfos(GameObject go, List<string> selectedAnims, int fps,
            out List<AnimationBakeInfo> bakeInfos,
            out Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransitions,
            out Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvents)
        {
            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            var animator = go.GetComponentInChildren<Animator>();
            animator.applyRootMotion = true;

            var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            Debug.Assert(controller != null && controller.layers.Length > 0);
            var layer = controller.layers[0];

            bakeInfos = new();
            cacheTransitions = new();
            cacheAnimationEvents = new();

            var selectedAnimsDic = selectedAnims.ToDictionary(a => a, a => true);

            AnalyzeStateMachine(
                layer.stateMachine, animator, skinnedMeshRenderers, selectedAnimsDic,
                0, fps, 0,
                bakeInfos, cacheTransitions, cacheAnimationEvents);

        }

        private static void ResetAnimationController(
            Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransitions,
            Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvents)
        {
            foreach (var obj in cacheTransitions)
            {
                obj.Key.transitions = obj.Value;
            }
            foreach (var obj in cacheAnimationEvents)
            {
                UnityEditor.AnimationUtility.SetAnimationEvents(obj.Key, obj.Value);
            }
        }

        private static void GetExtraBones(
            GameObject generatedObject,
            List<string> selectedExtraBones,
            out List<Transform> extraBones, out List<Matrix4x4> extraBindPose)
        {
            extraBindPose = new List<Matrix4x4>(150);
            extraBones = new List<Transform>(150);

            var bakedTrans = generatedObject.GetComponentsInChildren<Transform>(true);
            var root = generatedObject.transform;
            foreach (var path in selectedExtraBones)
            {
                for (int i = 0; i != bakedTrans.Length; ++i)
                {
                    if (RuntimeHelper.GetTransformPath(root, bakedTrans[i]) == path)
                    {
                        extraBindPose.Add(bakedTrans[i].localToWorldMatrix);
                        extraBones.Add(bakedTrans[i]);
                        break;
                    }
                }
            }
        }

        static void AnalyzeStateMachine(
            UnityEditor.Animations.AnimatorStateMachine stateMachine,
            Animator animator,
            SkinnedMeshRenderer[] meshRender,
            Dictionary<string, bool> selectedAnims,
            int layer, int bakeFPS, int animationIndex,
            List<AnimationBakeInfo> bakeInfos,
            Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransition,
            Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvent)
        {

            for (int i = 0; i != stateMachine.states.Length; ++i)
            {
                var state = stateMachine.states[i];
                var clip = state.state.motion as AnimationClip;

                if (clip == null) continue;

                if (!selectedAnims.TryGetValue(clip.name, out bool needBake))
                    continue;

                foreach (var obj in bakeInfos)
                {
                    if (obj.info.animationName == clip.name)
                    {
                        needBake = false;
                        break;
                    }
                }

                if (!needBake)
                    continue;

                var bake = new AnimationBakeInfo
                {
                    length = clip.averageDuration,
                    animator = animator,
                    layer = layer
                };
                bake.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var tf = (int)(bake.length * bakeFPS + 0.5f) + 1;

                bake.info = new AnimationInfo
                {
                    animationName = clip.name,
                    animationNameHash = state.state.nameHash,
                    startFrameIndex = animationIndex,
                    totalFrame = Mathf.Clamp(tf, 1, tf),
                    fps = bakeFPS,
                    rootMotion = true,
                    wrapMode = clip.isLooping ? WrapMode.Loop : clip.wrapMode,
                };
                if (bake.info.rootMotion)
                {
                    bake.info.velocity = new Vector3[bake.info.totalFrame];
                    bake.info.angularVelocity = new Vector3[bake.info.totalFrame];
                }
                bakeInfos.Add(bake);

                animationIndex += bake.info.totalFrame;

                bake.info.eventList = new List<AnimationEvent>();
                foreach (var evt in clip.events)
                {
                    var aniEvent = new AnimationEvent
                    {
                        function = evt.functionName,
                        floatParameter = evt.floatParameter,
                        intParameter = evt.intParameter,
                        stringParameter = evt.stringParameter,
                        time = evt.time
                    };

                    if (evt.objectReferenceParameter != null)
                        aniEvent.objectParameter = evt.objectReferenceParameter.name;
                    else
                        aniEvent.objectParameter = "";
                    bake.info.eventList.Add(aniEvent);
                }

                cacheTransition.Add(state.state, state.state.transitions);
                state.state.transitions = null;
                cacheAnimationEvent.Add(clip, clip.events);
                UnityEngine.AnimationEvent[] tempEvent = new UnityEngine.AnimationEvent[0];
                UnityEditor.AnimationUtility.SetAnimationEvents(clip, tempEvent);
            }
            for (int i = 0; i != stateMachine.stateMachines.Length; ++i)
            {
                AnalyzeStateMachine(
                    stateMachine.stateMachines[i].stateMachine,
                    animator, meshRender, selectedAnims,
                    layer, bakeFPS, animationIndex,
                    bakeInfos, cacheTransition, cacheAnimationEvent);
            }
        }

        private static void GenerateAnimationPoseData(
            List<AnimationBakeInfo> animBakeInfos,
            Transform[] boneList,
            Matrix4x4[] bindPoseList,
            out List<AnimationInfo> animInfoList,
            out List<AnimationPoseData> animPoseDataList)
        {
            animInfoList = new List<AnimationInfo>();
            animPoseDataList = new List<AnimationPoseData>();

            var index = 0;
            AnimationBakeInfo animBakeInfo = null;

            if (animBakeInfos.Count == 0) return;

            while (true)
            {
                if (index < animBakeInfos.Count)
                {
                    if (animBakeInfo == null)
                    {
                        animBakeInfo = animBakeInfos[index++];

                        animBakeInfo.animator.gameObject.SetActive(true);
                        animBakeInfo.animator.Update(0);
                        animBakeInfo.animator.Play(animBakeInfo.info.animationNameHash);
                        animBakeInfo.animator.Update(0);
                        animBakeInfo.workingFrame = 0;

                        continue;
                    }
                }

                if (animBakeInfo != null)
                {
                    var poseData = GeneratePoseData(boneList, bindPoseList,
                        animBakeInfo.info.animationNameHash, animBakeInfo.workingFrame);

                    animPoseDataList.Add(poseData);

                    animBakeInfo.info.velocity[animBakeInfo.workingFrame] = animBakeInfo.animator.velocity;
                    animBakeInfo.info.angularVelocity[animBakeInfo.workingFrame] = animBakeInfo.animator.angularVelocity * Mathf.Rad2Deg;
                    animBakeInfo.workingFrame++;

                    if (animBakeInfo.workingFrame >= animBakeInfo.info.totalFrame)
                    {
                        animInfoList.Add(animBakeInfo.info);
                        if (animBakeInfo.animator != null)
                        {
                            animBakeInfo.animator.gameObject.transform
                                .SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        }
                        animBakeInfo = null;

                        if (index >= animBakeInfos.Count)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var deltaTime = animBakeInfo.length / (animBakeInfo.info.totalFrame - 1);
                    animBakeInfo.animator.Update(deltaTime);
                }
            }

        }

        private static AnimationPoseData GeneratePoseData(
            Transform[] boneList, Matrix4x4[] bindPoseList,
            int stateName, float stateTime)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GeneratePoseData()");

            var poseMatrices = CalculatePoseMatrices(boneList, bindPoseList);

            var generated = new AnimationPoseData
            {
                stateName = stateName,
                animationTime = stateTime,
                frameIndex = -1,
                poseMatrices = poseMatrices
            };

            UnityEngine.Profiling.Profiler.EndSample();

            return generated;
        }

        private static Matrix4x4[] CalculatePoseMatrices(Transform[] bonePose, Matrix4x4[] bindPose)
        {
            if (bonePose.Length == 0) return null;

            var root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            var rootMat = root.worldToLocalMatrix;
            var matrices = new Matrix4x4[bonePose.Length];
            for (int i = 0; i != bonePose.Length; ++i)
            {
                matrices[i] = rootMat * bonePose[i].localToWorldMatrix * bindPose[i];
            }
            return matrices;
        }

        private class AnimationBakeInfo
        {
            public Animator animator;
            public int workingFrame;
            public float length;
            public int layer;
            public AnimationInfo info;
        }

        public class AnimationPoseData
        {
            public float animationTime;
            public int stateName;
            public int frameIndex;
            public Matrix4x4[] poseMatrices;

            public static void CopyData(AnimationPoseData dst, AnimationPoseData src)
            {
                dst.animationTime = src.animationTime;
                dst.frameIndex = src.frameIndex;
                dst.stateName = src.stateName;
                dst.poseMatrices = src.poseMatrices;
            }
        }
    }
}
#endif