using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimationInstancing_v2
{
    public partial class AnimationInstancingRenderer
    {
#if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(AnimationInstancingRenderer))]
        private class _Editor : UnityEditor.Editor
        {
            private AnimationInstancingRenderer _renderer;
            public List<BoneInfo> _allBones;
            private bool _allBonesFoldout;

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                _renderer ??= target as AnimationInstancingRenderer;
                DrawAllBones();
                DrawLodInfoList();
                DrawBaker();
            }

            private void DrawBaker()
            {
                if (_renderer.InstancingData != null
                && _renderer.RootTransform != null)
                {
                    using (new UnityEditor.EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Rebake"))
                        {
                            var output = AnimationBaker.BakeWithAnimator(new AnimationBakerData(_renderer.InstancingData, _renderer.RootTransform.gameObject));
                            AnimationBakerWindow.SaveToExistingAsset(output, _renderer.InstancingData);
                        }

                        if (GUILayout.Button("BakerWindow"))
                        {
                            AnimationBakerWindow.OpenWindow(new AnimationBakerData(_renderer.InstancingData, _renderer.RootTransform.gameObject));
                        }
                    }
                }
            }

            private void DrawLodInfoList()
            {
                if (_renderer._lodInfoList == null) return;
                foreach (var lod in _renderer._lodInfoList)
                {
                    UnityEditor.EditorGUILayout.LabelField($"VertexCacheList:");
                    // for (int i = 0; i < lod.vertexCacheList.Length; i++)
                    // {
                    //     var vc = lod.vertexCacheList[i];
                    //     UnityEditor.EditorGUILayout.LabelField($"VertexCache {i} [{vc.GetHashCode()}]:");
                    //     UnityEditor.EditorGUILayout.ObjectField("->mesh", vc.mesh, typeof(Mesh), true);
                    //     foreach (var m in vc.materials)
                    //     {
                    //         UnityEditor.EditorGUILayout.ObjectField("->material", m, typeof(Material), true);
                    //     }

                    //     UnityEditor.EditorGUILayout.LabelField($"->bone weights ({vc.weight.Length}) {string.Join(",", vc.weight)}");
                    //     UnityEditor.EditorGUILayout.LabelField($"->bone indices ({vc.boneIndex.Length}) {string.Join(",", vc.boneIndex)}");
                    //     UnityEditor.EditorGUILayout.LabelField($"->instanceBlockList: {string.Join(",", vc.instanceBlockDic.Select(b => $"({b.Key}={b.Value.GetHashCode()})"))}");
                    // }
                    // UnityEditor.EditorGUILayout.LabelField($"MaterialBlockList:");
                    // for (int i = 0; i < lod.materialBlockList.Length; i++)
                    // {
                    //     var block = lod.materialBlockList[i];
                    //     UnityEditor.EditorGUILayout.LabelField($"Block {i}-{block.GetHashCode()}:");
                    //     UnityEditor.EditorGUILayout.LabelField($"->runtimePackageIndex: {string.Join(",", block.runtimePackageCursors)}");
                    //     UnityEditor.EditorGUILayout.LabelField($"->packageLists: {block.packageLists.Length}");
                    //     foreach (var pl in block.packageLists)
                    //     {
                    //         UnityEditor.EditorGUILayout.LabelField($"->->packageList: {pl.Count}");
                    //         foreach (var p in pl)
                    //         {
                    //             UnityEditor.EditorGUILayout.LabelField($"->->->package: instancingCount={p.instancingCount} subMeshCount={p.subMeshCount}");
                    //         }
                    //     }
                    // }
                }
            }

            private void DrawAllBones()
            {
                if (_renderer.RootTransform == null || _renderer.InstancingData == null) return;
                if (_allBones == null)
                {
                    _allBones = new();

                    foreach (var smr in _renderer.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        foreach (var b in smr.bones)
                        {
                            var path = RuntimeHelper.GetTransformPath(_renderer.RootTransform, b);
                            _allBones.Add(new BoneInfo
                            {
                                bonePath = $"[{smr.name}]" + path,
                                boneTransform = b,
                                associatedRenderers = new List<Renderer>() { smr }
                            });
                        }
                    }

                    var extraBoneInfo = _renderer.InstancingData.boneData;

                    foreach (string path in extraBoneInfo.extraBones)
                    {
                        var found = RuntimeHelper.GetTransformAtPath(_renderer.RootTransform, path.Split("/"));
                        if (found != null)
                        {
                            var associatedRenderers = new List<Renderer>();
                            for (var i = 0; i < found.childCount; i++)
                            {
                                var mr = found.GetChild(i).GetComponent<MeshRenderer>();
                                if (mr != null)
                                {
                                    associatedRenderers.Add(mr);
                                }
                            }
                            _allBones.Add(new BoneInfo
                            {
                                bonePath = "[Extra]" + path,
                                boneTransform = found,
                                associatedRenderers = associatedRenderers,
                            });
                        }
                    }
                }

                _allBonesFoldout = UnityEditor.EditorGUILayout.Foldout(_allBonesFoldout, "All bones:", true);

                if (_allBonesFoldout)
                {
                    var i = 0;
                    foreach (var bone in _allBones)
                    {
                        UnityEditor.EditorGUILayout.ObjectField($"{i++} {bone.bonePath}", bone.boneTransform, typeof(Transform), true);
                        foreach (var r in bone.associatedRenderers)
                        {
                            UnityEditor.EditorGUILayout.ObjectField($"      ", r, typeof(Renderer), true);
                        }
                    }
                }
            }
        }

        private class BoneInfo
        {
            public string bonePath;
            public Transform boneTransform;
            public List<Renderer> associatedRenderers;
        }

        private class AnimationBakerData : IAnimationBakeData
        {
            private readonly AnimationInstancingData instancingData;

            public AnimationBakerData(AnimationInstancingData instancingData, GameObject prefab)
            {
                this.instancingData = instancingData;
                Prefab = prefab;
                SelectedExtraBones = instancingData.boneData.extraBones.ToList();
                SelectedAnims = instancingData.animInfoList.Select(a => a.animationName).ToList();
                Fps = instancingData.animInfoList.FirstOrDefault()?.fps ?? 30;
            }

            public GameObject Prefab { get; }
            public List<string> SelectedExtraBones { get; }
            public List<string> SelectedAnims { get; }
            public int Fps { get; }
            public AnimationInstancingData Asset => instancingData;
        }
    }
#endif
}