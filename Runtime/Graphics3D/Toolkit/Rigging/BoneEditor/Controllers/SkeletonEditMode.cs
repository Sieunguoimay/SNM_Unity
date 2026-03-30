#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.Graphics3D.Rigging
{
    /// <summary>
    /// Skeleton editing mode: create, select, move, and delete bones in the scene view.
    /// Left-click empty space = create bone at raycast hit (or screen-center fallback).
    /// Click bone gizmo = select. Shift+click = create child of selected bone.
    /// PositionHandle on selected bone for moving. Delete key removes selected bone.
    /// </summary>
    public class SkeletonEditMode : IToolMode
    {
        private RigDocument _doc;
        private Material _meshPreviewMaterial;

        public string DisplayName => "Skeleton";

        public void OnEnter(RigDocument doc)
        {
            _doc = doc;
            CreateMeshPreviewMaterial();
        }

        public void OnExit()
        {
            _doc = null;
            if (_meshPreviewMaterial != null)
            {
                Object.DestroyImmediate(_meshPreviewMaterial);
                _meshPreviewMaterial = null;
            }
        }

        private void CreateMeshPreviewMaterial()
        {
            if (_meshPreviewMaterial != null) Object.DestroyImmediate(_meshPreviewMaterial);
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            _meshPreviewMaterial = new Material(shader);
            _meshPreviewMaterial.SetInt("_ZWrite", 1);
            _meshPreviewMaterial.SetInt("_Cull", 0);
            _meshPreviewMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        public bool OnKeyDown(KeyCode key)
        {
            if (_doc == null) return false;

            if (key == KeyCode.Delete || key == KeyCode.X)
            {
                if (_doc.selectedBoneIndex >= 0)
                {
                    _doc.RemoveBone(_doc.selectedBoneIndex);
                    SceneView.RepaintAll();
                    return true;
                }
            }

            return false;
        }

        public void OnSceneGUI(SceneView view)
        {
            if (_doc == null) return;

            // Draw source mesh so user can see where to place bones
            if (_doc.sourceMesh != null && _meshPreviewMaterial != null)
            {
                for (int sub = 0; sub < _doc.sourceMesh.subMeshCount; sub++)
                    Graphics.DrawMesh(_doc.sourceMesh, Matrix4x4.identity, _meshPreviewMaterial,
                        0, view.camera, sub);
            }

            // Draw all bones
            BoneGizmoDrawer.DrawAllBones(_doc, Matrix4x4.identity);

            // Handle bone selection via clickable sphere buttons
            int clickedBone = DrawBoneButtons();

            // Position handle for selected bone
            if (_doc.selectedBoneIndex >= 0 && _doc.selectedBoneIndex < _doc.bones.Count)
            {
                var pos = _doc.GetBoneWorldPosition(_doc.selectedBoneIndex);
                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(pos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    UndoHelper.Record(_doc, "Move Bone");
                    _doc.SetBoneWorldPosition(_doc.selectedBoneIndex, newPos);
                }
            }

            // Handle mouse clicks for creating / selecting bones
            HandleMouseInput(view, clickedBone);

            // Draw help text in bottom-left (#3)
            Handles.BeginGUI();
            var helpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 0.8f) },
                alignment = TextAnchor.LowerLeft
            };
            var helpRect = new Rect(8f, view.position.height - 40f, 500f, 20f);
            GUI.Label(helpRect, "Ctrl+Click = Create Bone | Shift+Click = Add Child | X = Delete | Drag = Move", helpStyle);
            Handles.EndGUI();
        }

        /// <summary>
        /// Draws clickable sphere buttons at each bone position. Returns the index
        /// of the clicked bone, or -1 if none was clicked.
        /// </summary>
        private int DrawBoneButtons()
        {
            int clickedBone = -1;

            for (int i = 0; i < _doc.bones.Count; i++)
            {
                var pos = _doc.GetBoneWorldPosition(i);
                float handleSize = HandleUtility.GetHandleSize(pos) * 0.06f;
                bool isSelected = (i == _doc.selectedBoneIndex);

                Handles.color = isSelected ? Color.yellow : _doc.bones[i].displayColor;

                if (Handles.Button(pos, Quaternion.identity, handleSize, handleSize * 1.2f, Handles.SphereHandleCap))
                {
                    clickedBone = i;
                }
            }

            return clickedBone;
        }

        private void HandleMouseInput(SceneView view, int clickedBone)
        {
            var e = Event.current;

            // If a bone button was clicked, handle selection or child creation
            if (clickedBone >= 0)
            {
                if (e.shift)
                {
                    // Shift+click on bone = create child
                    CreateChildBone(clickedBone);
                }
                else
                {
                    // Regular click = select
                    UndoHelper.Record(_doc, "Select Bone");
                    _doc.selectedBoneIndex = clickedBone;
                }
                return;
            }

            // Left-click in empty space = create new root bone or child of selected
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                // Only handle if not over a handle
                if (GUIUtility.hotControl != 0) return;

                // Check if shift is held: create child of selected bone
                if (e.shift && _doc.selectedBoneIndex >= 0)
                {
                    var position = GetWorldPositionFromMouse(view);
                    CreateBoneAt(position, _doc.selectedBoneIndex);
                    e.Use();
                    return;
                }

                // If ctrl is held, create a root bone at mouse position
                if (e.control)
                {
                    var position = GetWorldPositionFromMouse(view);
                    CreateBoneAt(position, -1);
                    e.Use();
                }
            }
        }

        private void CreateChildBone(int parentIndex)
        {
            var parentPos = _doc.GetBoneWorldPosition(parentIndex);
            // Offset the child slightly from the parent
            var offset = Vector3.up * 0.2f;
            var childPos = parentPos + offset;

            int newIndex = _doc.AddBone("Bone_" + _doc.bones.Count, parentIndex, childPos);
            _doc.selectedBoneIndex = newIndex;
        }

        private void CreateBoneAt(Vector3 position, int parentIndex)
        {
            int newIndex = _doc.AddBone("Bone_" + _doc.bones.Count, parentIndex, position);
            _doc.selectedBoneIndex = newIndex;
        }

        private Vector3 GetWorldPositionFromMouse(SceneView view)
        {
            var mousePos = Event.current.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePos);

            // Raycast against mesh collider if available, otherwise use a plane
            if (_doc.sourceMesh != null)
            {
                // Try raycasting against physics scene
                if (Physics.Raycast(ray, out var hit, 1000f))
                    return hit.point;
            }

            // Fallback: intersect with XZ plane at Y=0 or at camera focus
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
                return ray.GetPoint(enter);

            // Last fallback: place at a fixed distance from camera
            return ray.GetPoint(5f);
        }
    }
}
#endif
