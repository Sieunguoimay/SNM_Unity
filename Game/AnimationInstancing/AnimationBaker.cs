using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimationInstancing_v2
{
    public class AnimationBaker
    {
        public void BakeWithAnimator(
            GameObject prefab,
            Dictionary<string, bool> selectedExtraBones,
            Dictionary<string, bool> selectedAnims)
        {
            if (prefab == null) return;
            var script = prefab.GetComponent<AnimationInstancing>();
            Debug.Assert(script);
            if (script == null) return;

            var go = Object.Instantiate(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            UnityEditor.Selection.activeGameObject = go;

            CreateBoneBakeInfos(prefab, go, selectedExtraBones,
                out var boneList,
                out var bindPoseList,
                out var extraBoneInfo);
            CreateAnimationBakeInfos(go, selectedAnims,
                out var bakeInfos,
                out var cacheTransitions,
                out var cacheAnimationEvents);
            GenerateAnimationBakeData(bakeInfos, boneList, bindPoseList,
                out var poseMatrices,
                out var animInfoList,
                out var generatedObjectInfoList);
            ResetAnimationController(cacheTransitions, cacheAnimationEvents);
            GenerateTextures(poseMatrices, animInfoList, generatedObjectInfoList, boneList.Length,
                out var bakedBoneTextures);

            BakedAnimationSaver.SaveAll(animInfoList, extraBoneInfo, bakedBoneTextures);
        }

        private static void GenerateTextures(
            Dictionary<int, ArrayList> poseMatrices,
            List<AnimationInfo> animInfoList,
            List<GenerateObjectInfo> generatedObjectInfoList,
            int boneCount,
            out Texture2D[] bakedBoneTextures)
        {
            AnimationTextureBaker.PrepareBoneTexture(
                animInfoList,
                4, boneCount,
                out bakedBoneTextures);

            AnimationTextureBaker.SetupAnimationTexture(
                animInfoList,
                generatedObjectInfoList,
                poseMatrices,
                bakedBoneTextures,
                4, boneCount);
        }

        private void GenerateAnimationBakeData(
            List<AnimationBakeInfo> animBakeInfos,
            Transform[] boneList,
            Matrix4x4[] bindPoseList,
            out Dictionary<int, ArrayList> poseMatrices,
            out List<AnimationInfo> animInfoList,
            out List<GenerateObjectInfo> generatedObjectInfoList)
        {
            poseMatrices = new Dictionary<int, ArrayList>();
            animInfoList = new List<AnimationInfo>();
            generatedObjectInfoList = new List<GenerateObjectInfo>();

            var index = 0;
            AnimationBakeInfo animBakeInfo = null;

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
                    var generated = GenerateBoneMatrix(boneList, bindPoseList,
                        animBakeInfo.info.animationNameHash, animBakeInfo.workingFrame);

                    AddPoseMatrixToPool(poseMatrices, animBakeInfo.info.animationNameHash, generated);

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

        private GenerateObjectInfo GenerateBoneMatrix(
            Transform[] boneList, Matrix4x4[] bindPoseList,
            int stateName, float stateTime)
        {
            UnityEngine.Profiling.Profiler.BeginSample("AddBoneMatrix()");

            var poseMatrix = CalculateSkinMatrix(boneList, bindPoseList);

            var generated = new GenerateObjectInfo
            {
                nameCode = -1,
                stateName = stateName,
                animationTime = stateTime,
                worldMatrix = Matrix4x4.identity,
                frameIndex = -1,
                boneListIndex = -1,
                boneMatrix = poseMatrix
            };

            UnityEngine.Profiling.Profiler.EndSample();

            return generated;
        }

        private static void AddPoseMatrixToPool(Dictionary<int, ArrayList> poseMatrices, int stateName, GenerateObjectInfo matrixData)
        {
            var data = new GenerateObjectInfo();

            GenerateObjectInfo.CopyMatrixData(data, matrixData);

            if (poseMatrices.ContainsKey(stateName))
            {
                poseMatrices[stateName].Add(data);
            }
            else
            {
                poseMatrices[stateName] = new ArrayList() { data };
            }
        }

        private static void CreateBoneBakeInfos(
            GameObject prefab, GameObject go,
            Dictionary<string, bool> selectedExtraBones,
            out Transform[] boneList,
            out Matrix4x4[] bindPoseList,
            out ExtraBoneInfo extraBoneInfo)
        {
            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            RuntimeHelper.MergeBone(skinnedMeshRenderers, out var mainBones, out var mainBindPoses);
            GetExtraBones(prefab, go, selectedExtraBones, out var extraBones, out var extraBindPoses);

            boneList = mainBones.Concat(extraBones).ToArray();
            bindPoseList = mainBindPoses.Concat(extraBindPoses).ToArray();
            extraBoneInfo = new ExtraBoneInfo
            {
                extraBoneNames = extraBones.Select(b => b.name).ToArray(),
                extraBindPoses = extraBindPoses.ToArray()
            };
        }

        private static void CreateAnimationBakeInfos(GameObject go, Dictionary<string, bool> selectedAnims,
            out List<AnimationBakeInfo> bakeInfos,
            out Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransitions,
            out Dictionary<AnimationClip, UnityEngine.AnimationEvent[]> cacheAnimationEvents)
        {
            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            var script = go.GetComponent<AnimationInstancing>();
            var animator = go.GetComponentInChildren<Animator>();
            animator.applyRootMotion = true;

            var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            Debug.Assert(controller.layers.Length > 0);
            var layer = controller.layers[0];

            bakeInfos = new();
            cacheTransitions = new();
            cacheAnimationEvents = new();

            AnalyzeStateMachine(
                layer.stateMachine, animator, skinnedMeshRenderers, script, selectedAnims,
                0, 15, 0,
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
            GameObject prefab,
            Dictionary<string, bool> selectedExtraBones,
            out List<Transform> listTransform, out List<Matrix4x4> bindPose)
        {
            bindPose = new List<Matrix4x4>(150);
            listTransform = new List<Transform>(150);

            var trans = prefab.GetComponentsInChildren<Transform>();
            var bakedTrans = generatedObject.GetComponentsInChildren<Transform>();

            foreach (var obj in selectedExtraBones)
            {
                if (!obj.Value) continue;

                for (int i = 0; i != trans.Length; ++i)
                {
                    var tran = trans[i];
                    if (tran.name == obj.Key)
                    {
                        bindPose.Add(tran.localToWorldMatrix);
                        listTransform.Add(bakedTrans[i]);
                    }
                }
            }
        }

        static void AnalyzeStateMachine(
            UnityEditor.Animations.AnimatorStateMachine stateMachine,
            Animator animator,
            SkinnedMeshRenderer[] meshRender,
            AnimationInstancing instance,
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
                    animationIndex = animationIndex,
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
                    animator, meshRender, instance, selectedAnims,
                    layer, bakeFPS, animationIndex,
                    bakeInfos, cacheTransition, cacheAnimationEvent);
            }
        }

        public static Matrix4x4[] CalculateSkinMatrix(Transform[] bonePose, Matrix4x4[] bindPose)
        {
            if (bonePose.Length == 0) return null;

            var root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            var rootMat = root.worldToLocalMatrix;
            var matrix = new Matrix4x4[bonePose.Length];
            for (int i = 0; i != bonePose.Length; ++i)
            {
                matrix[i] = rootMat * bonePose[i].localToWorldMatrix * bindPose[i];
            }
            return matrix;
        }

        private class AnimationBakeInfo
        {
            public Animator animator;
            public int workingFrame;
            public float length;
            public int layer;
            public AnimationInfo info;
        }

        public class GenerateObjectInfo
        {
            public Matrix4x4 worldMatrix;
            public int nameCode;
            public float animationTime;
            public int stateName;
            public int frameIndex;
            public int boneListIndex = -1;
            public Matrix4x4[] boneMatrix;

            public static void CopyMatrixData(GenerateObjectInfo dst, GenerateObjectInfo src)
            {
                dst.animationTime = src.animationTime;
                dst.boneListIndex = src.boneListIndex;
                dst.frameIndex = src.frameIndex;
                dst.nameCode = src.nameCode;
                dst.stateName = src.stateName;
                dst.worldMatrix = src.worldMatrix;
                dst.boneMatrix = src.boneMatrix;
            }
        }
    }

    public class ExtraBoneInfo
    {
        public string[] extraBoneNames;
        public Matrix4x4[] extraBindPoses;
    }

    public class AnimationInfo
    {
        public string animationName;
        public int animationNameHash;
        public int totalFrame;
        public int fps;
        public int animationIndex;
        public int textureIndex;
        public bool rootMotion;
        public WrapMode wrapMode;
        public Vector3[] velocity;
        public Vector3[] angularVelocity;
        public List<AnimationEvent> eventList;
    }

    public class AnimationEvent
    {
        public string function;
        public int intParameter;
        public float floatParameter;
        public string stringParameter;
        public string objectParameter;
        public float time;
    }

    public class MaterialBlock
    {
        public InstanceData instanceData;
        public int[] runtimePackageIndex;
        // array[index base on texture][package index]
        public List<InstancingPackage>[] packageList;
    }

    public class InstanceData
    {
        public List<Matrix4x4[]>[] worldMatrix;
        public List<float[]>[] frameIndex;
        public List<float[]>[] preFrameIndex;
        public List<float[]>[] transitionProgress;
    }

    public class InstancingPackage
    {
        public Material[] material;
        public int animationTextureIndex = 0;
        public int subMeshCount = 1;
        public int instancingCount;
        public int size;
        public MaterialPropertyBlock propertyBlock;
    }
}