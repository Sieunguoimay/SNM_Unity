using System;
using UnityEditor;
using UnityEngine;

namespace Snm.Runtime.GrassSystem
{
    public class GrassDebugTool : IDisposable
    {
        private SceneTextureDrawer _drawer;
        private readonly WorldCanvas worldCanvas;
        private readonly GrassSystemConfig systemConfig;

        public GrassDebugTool(WorldCanvas worldCanvas, GrassSystemConfig systemConfig)
        {
            this.worldCanvas = worldCanvas;
            this.systemConfig = systemConfig;
        }

        public void Dispose()
        {
            if (_drawer != null)
            {
                HideWindMap();
            }
        }

        public void ToggleWindMap()
        {
            if (_drawer == null)
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
            _drawer = new SceneTextureDrawer();
            SceneView.duringSceneGui += DrawScene;
        }

        public void HideWindMap()
        {
            SceneView.duringSceneGui -= DrawScene;
            _drawer.Dispose();
            _drawer = null;
        }

        void DrawScene(SceneView view)
        {
            var canvasPos = (worldCanvas.worldMax + worldCanvas.worldMin) / 2f;
            var canvasSize = worldCanvas.worldMax - worldCanvas.worldMin;

            _drawer.Draw(
                systemConfig.windConfig.dudvMap,
                position: new Vector3(canvasPos.x, 0, canvasPos.y),
                rotation: Quaternion.LookRotation(Vector3.down),
                size: canvasSize,
                textureScale: systemConfig.windConfig.mapScale
            );
        }
    }
}