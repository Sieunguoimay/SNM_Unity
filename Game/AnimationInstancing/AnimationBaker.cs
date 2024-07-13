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
            Dictionary<string, bool> selectedAnims
        )
        {
            if (prefab == null) return;
            var script = prefab.GetComponent<AnimationInstancing>();
            Debug.Assert(script);
            if (script == null) return;

            var go = Object.Instantiate(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            UnityEditor.Selection.activeGameObject = go;

            var skinnedMeshRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var s in skinnedMeshRenderers)
            {
                s.enabled = true;
            }

            RuntimeHelper.MergeBone(skinnedMeshRenderers, out var mainBones, out var mainBindPoses);
            GetExtraBones(prefab, go, selectedExtraBones, out var extraBones, out var extraBindPoses);

            var bones = mainBones.Concat(extraBones).ToArray();
            var bindPoses = mainBindPoses.Concat(extraBindPoses).ToArray();

            var extraBoneInfo = new ExtraBoneInfo
            {
                extraBoneNames = extraBones.Select(b => b.name).ToArray(),
                extraBindPoses = extraBindPoses.ToArray()
            };

            var vertexCache = new VertexCache()
            {
                nameCode = prefab.name.GetHashCode(),
                bonePose = bones,
                bindPose = bindPoses,
            };

            var animator = go.GetComponentInChildren<Animator>();
            animator.applyRootMotion = true;

            UnityEditor.Animations.AnimatorController controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
            Debug.Assert(controller.layers.Length > 0);
            UnityEditor.Animations.AnimatorControllerLayer layer = controller.layers[0];

            var bakeInfos = new List<AnimationBakeInfo>();
            var cacheTransitions = new Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]>();
            var cacheAnimationEvents = new Dictionary<AnimationClip, UnityEngine.AnimationEvent[]>();

            AnalyzeStateMachine(
                layer.stateMachine, animator, skinnedMeshRenderers, script, selectedAnims,
                0, 15, 0,
                bakeInfos, cacheTransitions, cacheAnimationEvents);
  
            var totalFrames = bakeInfos.Sum(b => b.info.totalFrame);
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
                if (!obj.Value)
                    continue;

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
            int layer,
            int bakeFPS,
            int animationIndex,
            List<AnimationBakeInfo> bakeInfos,
            Dictionary<UnityEditor.Animations.AnimatorState, UnityEditor.Animations.AnimatorStateTransition[]> cacheTransition,
            Dictionary<AnimationClip, UnityEngine.AnimationEvent[]>cacheAnimationEvent
            )
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
                    meshRender = meshRender,
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

        private class AnimationBakeInfo
        {
            public SkinnedMeshRenderer[] meshRender;
            public Animator animator;
            public int workingFrame;
            public float length;
            public int layer;
            public AnimationInfo info;
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

    public class VertexCache
    {
        public int nameCode;
        public Mesh mesh = null;
        public Dictionary<int, MaterialBlock> instanceBlockList;
        public Vector4[] weight;
        public Vector4[] boneIndex;
        public Material[] materials = null;
        public Matrix4x4[] bindPose;
        public Transform[] bonePose;
        public int boneTextureIndex = -1;

        // these are temporary, should be moved to InstancingPackage
        public ShadowCastingMode shadowcastingMode;
        public bool receiveShadow;
        public int layer;
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