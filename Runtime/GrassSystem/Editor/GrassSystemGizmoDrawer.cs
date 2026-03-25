using UnityEditor;
using UnityEngine;

namespace Snm.GrassSystem.Editor
{
    public static class GrassSystemGizmoDrawer
    {
        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmos(GrassSystem grassSystem, GizmoType gizmoType)
        {
            var config = grassSystem.Config;
            var canvas = grassSystem.Canvas;
            var matrices = grassSystem.Matrices;

            float bladeH = config.bladeHeight;
            float interactH = config.interactionHeight;

            Vector3 center;
            Vector3 size;

            if (canvas != null)
            {
                center = canvas.Position;
                size = new Vector3(canvas.Size.x, 0f, canvas.Size.y);
            }
            else
            {
                float totalWidth = (config.gridSize.x - 1) * config.cellSpacing.x;
                float totalDepth = (config.gridSize.y - 1) * config.cellSpacing.y;
                center = grassSystem.transform.position;
                size = new Vector3(totalWidth, 0f, totalDepth);
            }

            float hx = size.x * 0.5f;
            float hz = size.z * 0.5f;
            var corners = new[]
            {
                center + new Vector3(-hx, 0f, -hz),
                center + new Vector3( hx, 0f, -hz),
                center + new Vector3( hx, 0f,  hz),
                center + new Vector3(-hx, 0f,  hz),
            };

            DrawGroundPlane(center, size);
            DrawHeightPlane(center, size, corners, bladeH,
                new Color(0.3f, 0.9f, 0.3f, 0.1f), new Color(0.3f, 0.9f, 0.3f, 0.6f));
            DrawHeightPlane(center, size, corners, interactH,
                new Color(1f, 1f, 0.2f, 0.1f), new Color(1f, 1f, 0.2f, 0.6f));

            // Labels
            Handles.color = new Color(0.3f, 0.9f, 0.3f);
            Handles.Label(center + new Vector3(hx, bladeH, hz), $"Blade Height: {bladeH:F2}");
            Handles.color = Color.yellow;
            Handles.Label(center + new Vector3(-hx, interactH, hz), $"Interaction Height: {interactH:F2}");

            DrawBladeMeshPreview(config, center, grassSystem.transform.rotation);
            DrawBladePositions(config, matrices, grassSystem.transform);
        }

        static void DrawGroundPlane(Vector3 center, Vector3 size)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(center, size);
        }

        static void DrawHeightPlane(Vector3 center, Vector3 size, Vector3[] corners,
            float height, Color fillColor, Color wireColor)
        {
            var top = center + Vector3.up * height;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(top, size);
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(top, size);
            foreach (var c in corners)
                Gizmos.DrawLine(c, c + Vector3.up * height);
        }

        static void DrawBladeMeshPreview(GrassSystemConfig config, Vector3 center, Quaternion rotation)
        {
            if (config.grassMesh == null) return;

            Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawMesh(config.grassMesh, center, rotation);
            Gizmos.color = new Color(0f, 0.4f, 0f, 0.8f);
            Gizmos.DrawWireMesh(config.grassMesh, center, rotation);
        }

        static void DrawBladePositions(GrassSystemConfig config, Matrix4x4[] matrices, Transform transform)
        {
            if (matrices != null)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.8f);
                float dotSize = Mathf.Min(config.cellSpacing.x, config.cellSpacing.y) * 0.15f;
                for (int i = 0; i < matrices.Length; i++)
                    Gizmos.DrawSphere(matrices[i].GetPosition(), dotSize);
            }
            else
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.6f);
                int sizeX = config.gridSize.x;
                int sizeZ = config.gridSize.y;
                float spX = config.cellSpacing.x;
                float spZ = config.cellSpacing.y;
                float tw = (sizeX - 1) * spX;
                float td = (sizeZ - 1) * spZ;
                Vector3 pivot = new(-tw * 0.5f, 0f, -td * 0.5f);
                float dotSize = Mathf.Min(spX, spZ) * 0.15f;
                for (int z = 0; z < sizeZ; z++)
                    for (int x = 0; x < sizeX; x++)
                    {
                        var localPos = new Vector3(x * spX, 0f, z * spZ) + pivot;
                        Gizmos.DrawSphere(transform.TransformPoint(localPos), dotSize);
                    }
            }
        }
    }
}
