#if UNITY_EDITOR

using System;
using Snm.SurfaceInteraction;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugTool : IDisposable
    {
        private SceneTextureDrawer _dudvDrawer;
        private SceneTextureDrawer _trampleDrawer;
        private readonly SurfaceCanvas canvas;
        private readonly GrassSystemConfig systemConfig;
        private readonly RenderTexture trampleTexture;

        public RenderTexture TrampleTexture => trampleTexture;

        public GrassDebugTool(
            SurfaceCanvas canvas,
            GrassSystemConfig systemConfig,
            Texture trampleTexture)
        {
            this.canvas = canvas;
            this.systemConfig = systemConfig;
            this.trampleTexture = trampleTexture as RenderTexture;
        }

        public void Dispose()
        {
            if (_dudvDrawer != null)
            {
                HideWindMap();
            }
            if (_trampleDrawer != null)
            {
                HideTrampleMap();
            }
        }

        public void ToggleTrampleMap()
        {
            if (_trampleDrawer == null)
            {
                ShowTrampleMap();
            }
            else
            {
                HideTrampleMap();
            }
        }

        public void ToggleWindMap()
        {
            if (_dudvDrawer == null)
            {
                ShowWindMap();
            }
            else
            {
                HideWindMap();
            }
        }

        public void ShowWindMap()
        {
            _dudvDrawer = new SceneTextureDrawer();
            SceneView.duringSceneGui += DrawDudvToScene;
        }

        public void HideWindMap()
        {
            SceneView.duringSceneGui -= DrawDudvToScene;
            _dudvDrawer.Dispose();
            _dudvDrawer = null;
        }

        void DrawDudvToScene(SceneView view)
        {
            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var canvasPos = (max + min) / 2f;
            var canvasSize = max - min;

            _dudvDrawer.Draw(
                systemConfig.windConfig.dudvMap,
                position: new Vector3(canvasPos.x, 0, canvasPos.y),
                rotation: Quaternion.LookRotation(Vector3.down),
                size: canvasSize,
                textureScale: systemConfig.windConfig.mapScale
            );
        }

        public void ShowTrampleMap()
        {
            _trampleDrawer = new SceneTextureDrawer();
            SceneView.duringSceneGui += DrawTrampleToScene;
        }

        public void HideTrampleMap()
        {
            SceneView.duringSceneGui -= DrawTrampleToScene;
            _trampleDrawer.Dispose();
            _trampleDrawer = null;
        }

        void DrawTrampleToScene(SceneView view)
        {
            var min = canvas.WorldMin;
            var max = canvas.WorldMax;
            var canvasPos = (max + min) / 2f;
            var canvasSize = max - min;

            _trampleDrawer.Draw(
                trampleTexture,
                position: new Vector3(canvasPos.x, 0, canvasPos.y),
                rotation: Quaternion.LookRotation(Vector3.down),
                size: canvasSize,
                textureScale: Vector2.one
            );
        }
    }
}
#endif
