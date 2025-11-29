using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Snm.AnimationInstancing
{
    public partial class AnimationInstancingRenderer
    {
#if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(AnimationInstancingRenderer))]
        public class _Editor : UnityEditor.Editor
        {
            private AnimationInstancingRenderer _renderer;
            private List<BoneInfo> _allBones;
            private bool _allBonesFoldout;

            public override VisualElement CreateInspectorGUI()
            {
                var ve = new VisualElement();
                ve.Add(new IMGUIContainer(OnInspectorGUI));
                ve.Add(new InstancingDrawer(target as AnimationInstancingRenderer));
                return ve;
            }

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                _renderer ??= target as AnimationInstancingRenderer;
                DrawAllBones();
                DrawBaker();
            }

            private void DrawBaker()
            {
                using (new UnityEditor.EditorGUILayout.HorizontalScope())
                {
                    if (_renderer.InstancingData != null
                        && _renderer.RootTransform != null)
                    {
                        if (GUILayout.Button("Rebake"))
                        {
                            var output = AnimationBaker.BakeWithAnimator(
                                new AnimationBakerData(_renderer.InstancingData, _renderer.RootTransform.gameObject));
                            AnimationBakerWindow.SaveToExistingAsset(output, _renderer.InstancingData);
                        }
                    }

                    if (GUILayout.Button("BakerWindow"))
                    {
                        if (_renderer.RootTransform != null)
                        {
                            AnimationBakerWindow.OpenWindow(
                                new AnimationBakerData(_renderer.InstancingData, _renderer.RootTransform.gameObject));
                        }
                        else
                        {
                            AnimationBakerWindow.OpenWindow();
                        }
                    }
                }
            }

            private void DrawAllBones()
            {
                if (_renderer.RootTransform == null || _renderer.InstancingData == null) return;
                if (_allBones == null)
                {
                    _allBones = new();

                    foreach (var smr in _renderer.RootTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
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

                _allBonesFoldout = UnityEditor.EditorGUILayout.Foldout(_allBonesFoldout, $"All bones: ({_allBones.Count})", true);

                if (_allBonesFoldout)
                {
                    var i = 0;
                    foreach (var bone in _allBones)
                    {
                        UnityEditor.EditorGUILayout.ObjectField($"{i++} {bone.bonePath}", bone.boneTransform, typeof(Transform), true);
                        foreach (var r in bone.associatedRenderers)
                        {
                            if (r.sharedMaterial.shader.keywordSpace.keywords.Any(k => k.name == "SKINNED_INSTANCING_ON"))
                            {
                                UnityEditor.EditorGUILayout.ObjectField($"      ", r, typeof(Renderer), true);
                            }
                            else
                            {
                                var c = GUI.color;
                                GUI.color = Color.red;
                                UnityEditor.EditorGUILayout.ObjectField($"      [Unsupported Shader]", r, typeof(Renderer), true);
                                GUI.color = c;
                            }
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
                Prefab = prefab;
                if (instancingData != null)
                {
                    this.instancingData = instancingData;
                    SelectedExtraBones = instancingData.boneData.extraBones.ToList();
                    SelectedAnims = instancingData.animInfoList.Select(a => a.animationName).ToList();
                    Fps = instancingData.animInfoList.FirstOrDefault()?.fps ?? 30;
                }
            }

            public GameObject Prefab { get; }
            public List<string> SelectedExtraBones { get; }
            public List<string> SelectedAnims { get; }
            public int Fps { get; }
            public AnimationInstancingData Asset => instancingData;
        }

        public class InstancingDrawer : VisualElement
        {
            private readonly AnimationInstancingRenderer renderer;

            public InstancingDrawer(AnimationInstancingRenderer renderer)
            {
                this.renderer = renderer;
                CreateVE();
            }

            public void CreateVE()
            {
                if (renderer._lodInfoList == null) return;
                foreach (var lod in renderer._lodInfoList)
                {
                    Add(new VertexCacheListVE(lod.vertexCacheList, renderer.RootTransform));
                    Add(new MaterialBlockListVE(lod.materialBlockGroups));
                }
            }
        }

        public class VertexCacheListVE : Foldout
        {
            public VertexCacheListVE(IEnumerable<VertexCache> vertexCacheList, Transform root)
            {
                text = "Vertex Caches";
                var i = 0;
                foreach (var vc in vertexCacheList)
                {
                    Add(new VertexCacheVE(vc, root) { text = $"VertexCache {i++} {vc.name}", value = false });
                }
            }
        }

        public class VertexCacheVE : Foldout
        {
            private readonly Transform root;

            public VertexCacheVE(VertexCache vc, Transform root)
            {
                var line = new VisualElement();
                line.style.flexDirection = FlexDirection.Row;
                line.Add(new Label($"{vc.name} ({vc.nameHash})"));
                if (root != null)
                {
                    var btn = new Button() { text = "Ping" };
                    btn.RegisterCallback<ClickEvent>(evt => Ping(vc));
                    line.Add(btn);
                }
                Add(line);
                Add(new UnityEditor.UIElements.ObjectField()
                {
                    value = vc.mesh,
                    label = $"Mesh"
                });
                Add(new MaterialBlockListVE(vc.renderMaterialBlockGroupsDic.Values)
                { text = "MaterialBlocks", value = false });
                this.root = root;
            }

            private void Ping(VertexCache vc)
            {
                if (root != null)
                {
                    UnityEditor.EditorGUIUtility.PingObject(RuntimeHelper.GetTransformAtPath(root, vc.name.Split("/")));
                }
            }
        }

        private class MaterialBlockListVE : Foldout
        {
            public MaterialBlockListVE(IEnumerable<RenderMaterialBlockGroup> materialBlockList)
            {
                text = "Material Blocks";

                foreach (var mb in materialBlockList)
                {
                    Add(new Label($"sharedMaterials ({mb.OriginalSharedMaterials.Count}). clonedMaterialBlocks ({mb.RenderMaterialBlocks.Count})"));
                    for (int i = 0; i < mb.RenderMaterialBlocks.Count; i++)
                    {
                        var cmb = mb.RenderMaterialBlocks[i];
                        Add(new Label($"- ClonedBlock {i} instance Count: {cmb.totalInstancingCount}"));
                    }
                }
            }
        }

#endif
    }
}