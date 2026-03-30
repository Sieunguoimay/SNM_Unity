#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Test Pose mode: rotate bones interactively and preview live GPU-skinned deformation.
    /// Uses EditorSkinningPreview from BoneTool for rendering the deformed mesh.
    /// </summary>
    public class TestPoseMode : IToolMode
    {
        private static readonly string SkinShaderName = "Custom/GpuSkin";

        private RigDocument _doc;
        private EditorSkinningPreview _preview;
        private Material _plainMeshMaterial;
        private Matrix4x4[] _poseOffsets;       // Per-bone local rotation offset (applied on top of bind pose)
        private Matrix4x4[] _worldMatrices;     // Cached world-space matrices for posed bones
        private bool _isDirty = true;
        private bool _hasWeights;

        public string DisplayName => "Test Pose";

        public void OnEnter(RigDocument doc)
        {
            _doc = doc;

            int boneCount = doc.bones != null ? doc.bones.Count : 0;
            _poseOffsets = new Matrix4x4[boneCount];
            _worldMatrices = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
                _poseOffsets[i] = Matrix4x4.identity;

            _isDirty = true;
            _hasWeights = HasAnyWeights();
            if (_hasWeights) CreatePreview();
        }

        public void OnExit()
        {
            _preview?.Cleanup();
            _preview = null;
            if (_plainMeshMaterial != null) { Object.DestroyImmediate(_plainMeshMaterial); _plainMeshMaterial = null; }
            _doc = null;
            _poseOffsets = null;
            _worldMatrices = null;
        }

        private bool HasAnyWeights()
        {
            if (_doc?.vertexWeights == null) return false;
            for (int i = 0; i < _doc.vertexWeights.Length; i++)
                if (_doc.vertexWeights[i].TotalWeight > 0.001f) return true;
            return false;
        }

        public bool OnKeyDown(KeyCode key)
        {
            // 'R' to reset pose
            if (key == KeyCode.R)
            {
                ResetPose();
                return true;
            }

            return false;
        }

        public void OnSceneGUI(SceneView view)
        {
            if (_doc == null || _doc.bones == null || _doc.bones.Count == 0)
                return;

            int boneCount = _doc.bones.Count;

            // Ensure arrays are sized correctly (bones may have been added/removed)
            if (_poseOffsets == null || _poseOffsets.Length != boneCount)
            {
                _poseOffsets = new Matrix4x4[boneCount];
                _worldMatrices = new Matrix4x4[boneCount];
                for (int i = 0; i < boneCount; i++)
                    _poseOffsets[i] = Matrix4x4.identity;
                _isDirty = true;
            }

            // Show rotation handle for the selected bone
            if (_doc.selectedBoneIndex >= 0 && _doc.selectedBoneIndex < boneCount)
            {
                int selIdx = _doc.selectedBoneIndex;

                // Compute world matrices if dirty
                if (_isDirty)
                    ComputeWorldMatrices();

                var posedWorldMatrix = _worldMatrices[selIdx];
                var position = (Vector3)posedWorldMatrix.GetColumn(3);
                var rotation = posedWorldMatrix.rotation;

                EditorGUI.BeginChangeCheck();
                var newRotation = Handles.RotationHandle(rotation, position);
                if (EditorGUI.EndChangeCheck())
                {
                    // Compute the delta rotation and apply it to the pose offset
                    var delta = Quaternion.Inverse(rotation) * newRotation;
                    _poseOffsets[selIdx] = Matrix4x4.Rotate(delta) * _poseOffsets[selIdx];
                    _isDirty = true;
                }
            }

            // Recompute if needed
            if (_isDirty)
                ComputeWorldMatrices();

            // Draw bones at posed positions
            DrawPosedBones(boneCount);

            // Render GPU-skinned preview
            RenderPreview(boneCount);

            // Draw reset button in scene GUI
            Handles.BeginGUI();
            if (GUI.Button(new Rect(10, 10, 100, 25), "Reset Pose"))
                ResetPose();
            Handles.EndGUI();

            // Draw help text in bottom-left (#3)
            Handles.BeginGUI();
            var helpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f) },
                alignment = TextAnchor.LowerLeft
            };
            var helpRect = new Rect(8f, view.position.height - 40f, 500f, 20f);
            GUI.Label(helpRect, "Drag = Rotate Bone | R = Reset Pose", helpStyle);
            Handles.EndGUI();
        }

        /// <summary>
        /// Resets all pose offsets back to identity (bind pose).
        /// </summary>
        public void ResetPose()
        {
            if (_poseOffsets == null) return;

            for (int i = 0; i < _poseOffsets.Length; i++)
                _poseOffsets[i] = Matrix4x4.identity;

            _isDirty = true;
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Computes world matrices hierarchically, respecting parent-child relationships.
        /// World matrix for each bone = parentWorld * bindPoseInverse * poseOffset
        /// The final skinning matrix = worldMatrix * bindpose (to go from bind space to posed space).
        /// </summary>
        private void ComputeWorldMatrices()
        {
            int boneCount = _doc.bones.Count;

            for (int i = 0; i < boneCount; i++)
            {
                var bone = _doc.bones[i];
                var bindPoseInverse = bone.bindpose.inverse;

                // Local posed matrix: apply pose offset to the bone's local-from-bind transform
                // bindpose maps from world to bone-local, so bindpose.inverse is bone-local to world (at bind time)
                // We decompose: localTransform = parentBind * thisBind.inverse
                // Then apply poseOffset rotation in that local space.

                if (bone.parentIndex < 0)
                {
                    // Root bone: world = bindPoseInverse * poseOffset (pose offset is in bone-local space)
                    _worldMatrices[i] = bindPoseInverse * _poseOffsets[i];
                }
                else if (bone.parentIndex >= 0 && bone.parentIndex < boneCount && bone.parentIndex < i)
                {
                    // Child bone:
                    // localTransform = parentBindpose * thisBone.bindpose.inverse
                    // posedLocal = localTransform * poseOffset
                    // worldMatrix = parentWorldMatrix * posedLocal
                    var parentBind = _doc.bones[bone.parentIndex].bindpose;
                    var localTransform = parentBind * bindPoseInverse;
                    var posedLocal = localTransform * _poseOffsets[i];
                    _worldMatrices[i] = _worldMatrices[bone.parentIndex] * posedLocal;
                }
                else
                {
                    // Parent not yet computed or invalid: treat as root
                    _worldMatrices[i] = bindPoseInverse * _poseOffsets[i];
                }
            }

            _isDirty = false;
        }

        /// <summary>
        /// Draws bone connections and points at their posed positions.
        /// </summary>
        private void DrawPosedBones(int boneCount)
        {
            for (int i = 0; i < boneCount; i++)
            {
                var pos = (Vector3)_worldMatrices[i].GetColumn(3);
                var bone = _doc.bones[i];

                // Draw connection to parent
                if (bone.parentIndex >= 0)
                {
                    var parentPos = (Vector3)_worldMatrices[bone.parentIndex].GetColumn(3);
                    Handles.color = bone.displayColor;
                    Handles.DrawLine(parentPos, pos);
                }

                // Draw bone point
                float handleSize = HandleUtility.GetHandleSize(pos) * 0.05f;
                bool isSelected = (i == _doc.selectedBoneIndex);
                Handles.color = isSelected ? Color.yellow : bone.displayColor;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, handleSize * 2f, EventType.Repaint);

                // Draw label
                Handles.Label(pos + Vector3.up * handleSize * 3f, bone.name);
            }
        }

        /// <summary>
        /// Renders the mesh — GPU-skinned if weights exist, plain mesh otherwise.
        /// </summary>
        private void RenderPreview(int boneCount)
        {
            if (!_hasWeights)
            {
                DrawPlainMesh();
                return;
            }

            if (_preview == null || !_preview.IsReady)
            {
                CreatePreview();
                if (_preview == null || !_preview.IsReady)
                {
                    DrawPlainMesh();
                    return;
                }
            }

            // Skinning matrix for GPU: finalMatrix[i] = worldMatrix[i] * bindpose[i]
            for (int i = 0; i < boneCount; i++)
            {
                var skinMatrix = _worldMatrices[i] * _doc.bones[i].bindpose;
                _preview.SetSkinningMatrix(i, skinMatrix);
            }

            _preview.UploadAndRender(boneCount, Matrix4x4.identity);
        }

        private void DrawPlainMesh()
        {
            if (_doc.sourceMesh == null) return;
            if (_plainMeshMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) return;
                _plainMeshMaterial = new Material(shader);
                _plainMeshMaterial.SetInt("_ZWrite", 1);
                _plainMeshMaterial.SetInt("_Cull", 0);
                _plainMeshMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.4f));
            }
            for (int sub = 0; sub < _doc.sourceMesh.subMeshCount; sub++)
                Graphics.DrawMesh(_doc.sourceMesh, Matrix4x4.identity, _plainMeshMaterial, 0);
        }

        private void CreatePreview()
        {
            if (_doc == null || _doc.sourceMesh == null || _doc.bones == null || _doc.bones.Count == 0)
                return;

            var shader = Shader.Find(SkinShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[TestPoseMode] Could not find shader '{SkinShaderName}'. GPU preview disabled.");
                return;
            }

            // Clone mesh to avoid modifying the original, and bake current weights into it
            var previewMesh = Object.Instantiate(_doc.sourceMesh);
            previewMesh.name = _doc.sourceMesh.name + "_TestPosePreview";

            // Bake bone weights into the cloned mesh
            if (_doc.vertexWeights != null && _doc.vertexWeights.Length == previewMesh.vertexCount)
            {
                var boneWeights = new BoneWeight[previewMesh.vertexCount];
                for (int i = 0; i < boneWeights.Length; i++)
                    boneWeights[i] = _doc.vertexWeights[i].ToBoneWeight();
                previewMesh.boneWeights = boneWeights;
            }

            // Bake bindposes
            var bindposes = new Matrix4x4[_doc.bones.Count];
            for (int i = 0; i < bindposes.Length; i++)
                bindposes[i] = _doc.bones[i].bindpose;
            previewMesh.bindposes = bindposes;

            _preview?.Cleanup();
            _preview = new EditorSkinningPreview();
            _preview.Create(previewMesh, shader, _doc.bones.Count);
        }
    }
}
#endif
