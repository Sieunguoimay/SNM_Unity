#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Snm.GPUSkinning.BoneToolV2
{
    /// <summary>
    /// Static utility for drawing bone gizmos in the scene view.
    /// Draws lines from parent to child joints, spheres at joint positions,
    /// and labels with bone names.
    /// </summary>
    public static class BoneGizmoDrawer
    {
        /// <summary>
        /// Draws a single bone as a line from parent to child with a sphere at the joint.
        /// </summary>
        public static void DrawBone(Vector3 position, Vector3 parentPosition, Color color, bool isSelected)
        {
            float handleSize = HandleUtility.GetHandleSize(position) * 0.05f;

            // Draw connection line to parent
            if (parentPosition != position)
            {
                Handles.color = new Color(color.r, color.g, color.b, 0.7f);
                Handles.DrawLine(parentPosition, position, 2f);
            }

            // Draw joint sphere
            Handles.color = isSelected ? Color.yellow : color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, handleSize * 2f, EventType.Repaint);

            // Draw selection ring for selected bones
            if (isSelected)
            {
                Handles.color = new Color(1f, 1f, 0f, 0.3f);
                Handles.DrawWireDisc(position, Camera.current != null ? Camera.current.transform.forward : Vector3.forward, handleSize * 3f);
            }
        }

        /// <summary>
        /// Draws all bones in the document with labels and hierarchy lines.
        /// </summary>
        /// <param name="doc">The rig document containing bone data.</param>
        /// <param name="localToWorld">Transform matrix applied to bone positions.</param>
        public static void DrawAllBones(RigDocument doc, Matrix4x4 localToWorld)
        {
            if (doc == null || doc.bones == null) return;

            for (int i = 0; i < doc.bones.Count; i++)
            {
                var bone = doc.bones[i];
                var worldPos = localToWorld.MultiplyPoint3x4(doc.GetBoneWorldPosition(i));
                bool isSelected = (i == doc.selectedBoneIndex);

                // Get parent position
                Vector3 parentPos = worldPos;
                if (bone.parentIndex >= 0 && bone.parentIndex < doc.bones.Count)
                {
                    parentPos = localToWorld.MultiplyPoint3x4(doc.GetBoneWorldPosition(bone.parentIndex));
                }

                DrawBone(worldPos, parentPos, bone.displayColor, isSelected);

                // Draw label
                float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.05f;
                var labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    normal = { textColor = isSelected ? Color.yellow : Color.white }
                };
                Handles.Label(worldPos + Vector3.up * handleSize * 3f, $"[{i}] {bone.name}", labelStyle);
            }
        }
    }
}
#endif
