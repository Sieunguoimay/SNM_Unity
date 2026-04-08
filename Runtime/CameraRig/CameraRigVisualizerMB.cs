using UnityEngine;

namespace Snm.CameraRig
{
    public class CameraRigVisualizerMB : MonoBehaviour
    {
        private CameraRigVisualizeData _data;

        public void SetVisualizeData(CameraRigVisualizeData data)
        {
            _data = data;
        }

        private void OnGUI()
        {
            if (_data?.camera == null) return;

            var pixelSize = new Vector2Int(_data.camera.pixelWidth, _data.camera.pixelHeight);
            var sCombinedRect = BoundsUtility.BoundsNDCToScreenRect(_data.ndcBounds, pixelSize);

            DrawRect(sCombinedRect, Color.green);
        }

        void OnDrawGizmos()
        {
            if (_data?.camera == null) return;

            var vp = _data.camera.projectionMatrix * _data.camera.worldToCameraMatrix;
            var invVP = vp.inverse;

            DrawBounds(_data.ndcBounds, invVP, Color.green);

            if (_data.targets == null) return;
            foreach (var t in _data.targets)
            {
                Gizmos.DrawWireCube(t.VisibleBounds.center, t.VisibleBounds.size);
                if (t.DesiredCamDirection != null)
                {
                    Gizmos.DrawLine(t.VisibleBounds.center, t.VisibleBounds.center + Vector3.Scale(t.DesiredCamDirection.Value, t.VisibleBounds.size));
                }
            }
        }

        private void DrawRect(Rect screenRect, Color color)
        {
            var guiY = Screen.height - screenRect.yMax;
            var guiRect = new Rect(screenRect.xMin, guiY, screenRect.width, screenRect.height);

            var old = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMin, guiRect.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMax - 2, guiRect.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMin, guiRect.yMin, 2, guiRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(guiRect.xMax - 2, guiRect.yMin, 2, guiRect.height), Texture2D.whiteTexture);

            GUI.color = old;
        }

        void DrawBounds(Bounds ndcBounds, Matrix4x4 invVP, Color color)
        {
            var min = ndcBounds.min;
            var max = ndcBounds.max;

            var ndcCorners = new[]{
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(min.x, max.y, max.z),
            };

            var worldCorners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                worldCorners[i] = TransformPoint(ndcCorners[i], invVP);
            }

            var old = Gizmos.color;
            Gizmos.color = color;
            DrawBox(worldCorners);
            Gizmos.color = old;
        }

        private Vector3 TransformPoint(Vector3 point, Matrix4x4 invVP)
        {
            var clip = new Vector4(point.x, point.y, point.z, 1f);
            var world = invVP * clip;
            world /= world.w;
            return new Vector3(world.x, world.y, world.z);
        }

        void DrawBox(Vector3[] c)
        {
            Gizmos.DrawLine(c[0], c[1]);
            Gizmos.DrawLine(c[1], c[2]);
            Gizmos.DrawLine(c[2], c[3]);
            Gizmos.DrawLine(c[3], c[0]);

            Gizmos.DrawLine(c[4], c[5]);
            Gizmos.DrawLine(c[5], c[6]);
            Gizmos.DrawLine(c[6], c[7]);
            Gizmos.DrawLine(c[7], c[4]);

            Gizmos.DrawLine(c[0], c[4]);
            Gizmos.DrawLine(c[1], c[5]);
            Gizmos.DrawLine(c[2], c[6]);
            Gizmos.DrawLine(c[3], c[7]);
        }
    }
}
